using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BeatSaberTwitchOverlay
{
    public class TwitchDataUpdate
    {
        public TwitchDataUpdate() { }

        public string Action { get; set; }
        public long Timestamp { get; set; }
    }

    public class TwitchSongUpdate : TwitchDataUpdate
    {
        public TwitchSongUpdate() { }

        public string SongId { get; set; }
        public string SongName { get; set; }
        public string SongSubName { get; set; }
        public string AuthorName { get; set; }
        public string Difficulty { get; set; }
        public float SongLength { get; set; }
        public string GameMode { get; set; }
        public bool IsCustomSong { get; set; }
        public bool IsNoFail { get; set; }
        public bool IsMirror { get; set; }
        public int NotesCount { get; set; }
        public int ObstaclesCount { get; set; }
        public float BPM { get; set; }
        public SongLineData[] SongData { get; set; }
    }

    public class TwitchSceneUpdate : TwitchDataUpdate
    {
        public TwitchSceneUpdate() { }

        public MenuSceneSetupData.SceneState Scene { get; set; }
    }

    public class TwitchSongResults : TwitchSceneUpdate
    {
        public TwitchSongResults() { }

        public LevelCompletionResults Results { get; set; }
    }

    public class Plugin : IPlugin
	{
		private MainGameSceneSetupData _mainSetupData;
		private GameStaticData _gameStaticData;
        private ScoreController _scoreController;
		private bool _init;
		
		public string Name
		{
			get { return "Twitch Overlay Provider"; }
		}

		public string Version
		{
			get { return "0.1.0"; }
		}
		
		public void OnApplicationStart()
		{
			if (_init) return;
			_init = true;
			SceneManager.activeSceneChanged += SceneManagerOnActiveSceneChanged;
        }

		public void OnApplicationQuit()
		{
			SceneManager.activeSceneChanged -= SceneManagerOnActiveSceneChanged;
		}

		private void SceneManagerOnActiveSceneChanged(Scene oldScene, Scene newScene)
		{


            if (newScene.buildIndex == 1)
            {
                // Loaded menu scene
                System.Net.WebClient client = new System.Net.WebClient();
                
                var menuSceneSetupData = Resources.FindObjectsOfTypeAll<MenuSceneSetupData>().FirstOrDefault();

                if (menuSceneSetupData.sceneState == MenuSceneSetupData.SceneState.Results)
                {
                    // Send result data
                    var twitchResults = new TwitchSongResults
                    {
                        Action = "SongResults",
                        Results = menuSceneSetupData.levelCompletionResults,
                        Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
                    };

                    var p = new NameValueCollection();
                    p.Add("Action", "SongResults");
                    p.Add("Data", JsonConvert.SerializeObject(twitchResults));
                    p.Add("Timestamp", twitchResults.Timestamp.ToString());
                    
                    client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", p);
                } else
                {
                    var twitchData = new TwitchSceneUpdate
                    {
                        Action = "Menu",
                        Scene = menuSceneSetupData.sceneState,
                        Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
                    };

                    var p = new NameValueCollection();
                    p.Add("Action", "SongResults");
                    p.Add("Data", JsonConvert.SerializeObject(twitchData));
                    p.Add("Timestamp", twitchData.Timestamp.ToString());

                    client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", p);
                }
            }
            else if (newScene.buildIndex == 4)
            {
                // Loaded game playing scene
                _mainSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().FirstOrDefault();
                _gameStaticData = Resources.FindObjectsOfTypeAll<GameStaticData>().FirstOrDefault();
                _scoreController = Resources.FindObjectsOfTypeAll<ScoreController>().FirstOrDefault();
                
                if (_mainSetupData == null || _gameStaticData == null || _scoreController == null)
                {
                    Console.WriteLine("Twitch Overlay: Cannot load game data.");
                    return;
                }

                // Attach note event handlers
                _scoreController.noteWasCutEvent += HandleNoteWasCutEvent;
                _scoreController.noteWasMissedEvent += HandleNoteWasMissedEvent;

                var song = _gameStaticData.GetLevelData(_mainSetupData.levelId);
                var songData = song.GetDifficultyLevel(_mainSetupData.difficulty).songLevelData.songData;

                var twitchSongUpdate = new TwitchSongUpdate
                {
                    Action = "PlayingSong",
                    SongId = song.levelId,
                    SongName = song.songName,
                    SongSubName = song.songSubName,
                    AuthorName = song.authorName,
                    Difficulty = LevelStaticData.GetDifficultyName(_mainSetupData.difficulty),
                    SongLength = song.GetDifficultyLevel(_mainSetupData.difficulty).audioClip.length,
                    GameMode = GetGameplayModeName(_mainSetupData.gameplayMode),
                    IsCustomSong = song.levelId.Contains('∎'),
                    IsNoFail = _mainSetupData.gameplayOptions.noEnergy,
                    IsMirror = _mainSetupData.gameplayOptions.mirror,
                    NotesCount = songData.notesCount,
                    ObstaclesCount = songData.obstaclesCount,
                    BPM = songData.BeatsPerMinute,
                    SongData = songData.SongLinesData,
                    Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds
                };

                try
                {
                    var p = new NameValueCollection();
                    p.Add("Action", "PlayingSong");
                    p.Add("Data", JsonConvert.SerializeObject(twitchSongUpdate));
                    p.Add("Timestamp", twitchSongUpdate.Timestamp.ToString());

                    System.Net.WebClient client = new System.Net.WebClient();
                    client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", p);
                } catch (Exception e)
                {
                    var p = new NameValueCollection();
                    p.Add("state", "Error");
                    p.Add("Exception", e.ToString());
                    System.Net.WebClient client = new System.Net.WebClient();
                    client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", p);
                }

                
            }
        }

		public void OnLevelWasLoaded(int level)
		{
            var parameters = new NameValueCollection();
            parameters.Add("INFO", "Level Loaded");
            parameters.Add("level", level.ToString());
            parameters.Add("timestamp", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());


            System.Net.WebClient client = new System.Net.WebClient();
            client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", parameters);
        }

		public void OnLevelWasInitialized(int level)
		{
            var parameters = new NameValueCollection();
            parameters.Add("INFO", "Level Initialized");
            parameters.Add("level", level.ToString());
            parameters.Add("timestamp", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());


            System.Net.WebClient client = new System.Net.WebClient();
            client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", parameters);
        }

		public void OnUpdate()
		{

        }

		public void OnFixedUpdate()
		{

        }

        // public event Action<NoteData, NoteCutInfo, int> noteWasCutEvent;
        public void HandleNoteWasCutEvent(NoteData noteData, NoteCutInfo noteCutInfo, int number)
        {
            var parameters = new NameValueCollection();
            parameters.Add("Event", "Note Cut");

            parameters.Add("IsCutOK", noteCutInfo.speedOK && noteCutInfo.saberTypeOK && noteCutInfo.directionOK && !noteCutInfo.wasCutTooSoon && noteCutInfo.allIsOK ? "true" : "false");
            parameters.Add("Note ID", noteData.id.ToString());
            parameters.Add("timestamp", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());

            System.Net.WebClient client = new System.Net.WebClient();
            client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", parameters);
        }

        // public event Action<NoteData, int> noteWasMissedEvent;
        public void HandleNoteWasMissedEvent(NoteData noteData, int number)
        {
            var parameters = new NameValueCollection();
            parameters.Add("Event", "Note Missed");
            parameters.Add("Number", number.ToString());
            parameters.Add("Note ID", noteData.id.ToString());
            parameters.Add("timestamp", DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString());


            System.Net.WebClient client = new System.Net.WebClient();
            client.UploadValuesAsync(new System.Uri("http://localhost:3000/gamedata"), "POST", parameters);
        }

		public static string GetGameplayModeName(GameplayMode gameplayMode)
		{
			switch (gameplayMode)
			{
				case GameplayMode.SoloStandard:
					return "Solo Standard";
				case GameplayMode.SoloOneSaber:
					return "One Saber";
				case GameplayMode.SoloNoArrows:
					return "No Arrows";
				case GameplayMode.PartyStandard:
					return "Party";
				default:
					return "Solo Standard";
			}
		}

		private static string GetGameplayModeImage(GameplayMode gameplayMode)
		{
			switch (gameplayMode)
			{
				case GameplayMode.SoloStandard:
					return "solo";
				case GameplayMode.SoloOneSaber:
					return "one_saber";
				case GameplayMode.SoloNoArrows:
					return "no_arrows";
				case GameplayMode.PartyStandard:
					return "party";
				default:
					return "solo";
			}
		}
	}
}
 