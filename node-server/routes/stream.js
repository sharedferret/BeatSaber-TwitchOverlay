var express = require('express');
const path = require('path');
var router = express.Router();

/* GET webpage overlay */
router.get('/', function(req, res, next) {
    console.log('sse init');
    sse.init();
});

module.exports = router;
