var express = require('express');
var router = express.Router();

/* POST game data from Beat Saber plugin */
router.post('/', function(req, res, next) {
    // close the connection immediately
    res.send(JSON.stringify({ status: 'ok' }));

    // log output here
    console.log(req.body);

    // Forward via SSE
    sse.serialize(req.body);
});

module.exports = router;
