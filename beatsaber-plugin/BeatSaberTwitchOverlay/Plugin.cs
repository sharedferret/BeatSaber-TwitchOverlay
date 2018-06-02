using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using IllusionPlugin;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BeatSaberTwitchOverlay
{
    [DataContract]
    public class TwitchGameData
    {
        public TwitchGameData() { }

        [DataMember]
        public string State { get; set; }

        [DataMember]
        public string SongId { get; set; }

        [DataMember]
        public string SongName { get; set; }

        [DataMember]
        public string SongSubName { get; set; }

        [DataMember]
        public string AuthorName { get; set; }

        [DataMember]
        public string Difficulty { get; set; }

        [DataMember]
        public float SongLength { get; set; }

        [DataMember]
        public Sprite CoverImage { get; set; }

        [DataMember]
        public string GameMode { get; set; }

        [DataMember]
        public bool CustomSong { get; set; }

        [DataMember]
        public bool NoFail { get; set; }

        [DataMember]
        public bool Mirror { get; set; }

        [DataMember]
        public int NotesCount { get; set; }

        [DataMember]
        public int ObstaclesCount { get; set; }

        [DataMember]
        public float BPM { get; set; }

        [DataMember]
        public long Timestamp { get; set; }
    }


    public class Plugin : IPlugin
	{
		private MainGameSceneSetupData _mainSetupData;
		private GameStaticData _gameStaticData;
		private bool _init;
		
		public string Name
		{
			get { return "Twitch Overlay Provider"; }
		}

		public string Version
		{
			get { return "0.0.1"; }
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
                TwitchGameData twitchData = new TwitchGameData();
                twitchData.State = "Menu";
                twitchData.Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                var parameters = new NameValueCollection();
                parameters.Add("state", twitchData.State);
                parameters.Add("timestamp", twitchData.Timestamp.ToString());

                System.Net.WebClient client = new System.Net.WebClient();
                string response = Encoding.ASCII.GetString(client.UploadValues("http://localhost:3000/gamedata", "POST",
                    parameters));
            }
            else if (newScene.buildIndex == 4)
            {
                // Loaded game playing scene
                TwitchGameData twitchData = new TwitchGameData();

                _mainSetupData = Resources.FindObjectsOfTypeAll<MainGameSceneSetupData>().FirstOrDefault();
                _gameStaticData = Resources.FindObjectsOfTypeAll<GameStaticData>().FirstOrDefault();
                if (_mainSetupData == null || _gameStaticData == null)
                {
                    Console.WriteLine("Twitch Overlay: Cannot load game data.");
                    return;
                }

                var song = _gameStaticData.GetLevelData(_mainSetupData.levelId);

                twitchData.State = "PlayingSong";
                twitchData.SongId = song.levelId;
                twitchData.SongName = song.songName;
                twitchData.SongSubName = song.songSubName;
                twitchData.AuthorName = song.authorName;
                twitchData.Difficulty = LevelStaticData.GetDifficultyName(_mainSetupData.difficulty);
                twitchData.SongLength = song.GetDifficultyLevel(_mainSetupData.difficulty).audioClip.length;
                twitchData.CoverImage = song.coverImage;
                twitchData.GameMode = GetGameplayModeName(_mainSetupData.gameplayMode);
                twitchData.CustomSong = song.levelId.Contains('∎');
                twitchData.NoFail = _mainSetupData.gameplayOptions.noEnergy;
                twitchData.Mirror = _mainSetupData.gameplayOptions.mirror;
                twitchData.NotesCount = song.GetDifficultyLevel(_mainSetupData.difficulty).songLevelData.songData.notesCount;
                twitchData.ObstaclesCount = song.GetDifficultyLevel(_mainSetupData.difficulty).songLevelData.songData.obstaclesCount;
                twitchData.BPM = song.GetDifficultyLevel(_mainSetupData.difficulty).songLevelData.songData.BeatsPerMinute;
                twitchData.Timestamp = (long)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;

                var parameters = new NameValueCollection();
                parameters.Add("state", twitchData.State);
                parameters.Add("songId", twitchData.SongId);
                parameters.Add("songName", twitchData.SongName);
                parameters.Add("songSubName", twitchData.SongSubName);
                parameters.Add("authorName", twitchData.AuthorName);
                parameters.Add("difficulty", twitchData.Difficulty);
                parameters.Add("songLength", twitchData.SongLength.ToString());
                // parameters.Add("coverImage", twitchData.CoverImage.)
                parameters.Add("gameMode", twitchData.GameMode);
                parameters.Add("customSong", twitchData.CustomSong ? "true" : "false");
                parameters.Add("noFail", twitchData.NoFail ? "true" : "false");
                parameters.Add("mirror", twitchData.Mirror ? "true" : "false");
                parameters.Add("notesCount", twitchData.NotesCount.ToString());
                parameters.Add("obstaclesCount", twitchData.ObstaclesCount.ToString());
                parameters.Add("bpm", twitchData.BPM.ToString());
                parameters.Add("timestamp", twitchData.Timestamp.ToString());
                

                System.Net.WebClient client = new System.Net.WebClient();
                string response = Encoding.ASCII.GetString(client.UploadValues("http://localhost:3000/gamedata", "POST",
                    parameters));
            }
        }

		public void OnLevelWasLoaded(int level)
		{
			
		}

		public void OnLevelWasInitialized(int level)
		{
			
		}

		public void OnUpdate()
		{

		}

		public void OnFixedUpdate()
		{
			
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