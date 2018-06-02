var express = require('express');
const path = require('path');
var router = express.Router();

/* GET webpage overlay */
router.get('/', function(req, res, next) {
    res.sendFile(path.join(__dirname + '/../views/overlay.html'));
});

module.exports = router;
