var express = require('express');
var router = express.Router();

/* POST game data from Beat Saber plugin */
router.post('/', function(req, res, next) {
    // close the connection immediately
    res.send(JSON.stringify({ status: 'ok' }));

    // log output here
    const data = req.body.Data ? JSON.parse(req.body.Data) : req.body;
    console.log(data);

    // Forward via SSE
    sse.serialize(data);
});

module.exports = router;
