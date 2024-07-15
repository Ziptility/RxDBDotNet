"use strict";
var express = require("express");
var path = require("path");
var app = express();
var port = process.env.PORT || 1337;

// Serve static files from the public directory
app.use(express.static(path.join(__dirname, "public")));

// Serve the index.html file at the root URL
app.get("/", function(req, res) {
    res.sendFile(path.join(__dirname, "index.html"));
});

app.listen(port, function() {
    console.log(`Server running at http://localhost:${port}`);
});