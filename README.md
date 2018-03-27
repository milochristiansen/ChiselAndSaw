
RAMS: Remote Audio Metadata Server
========================================================================================================================

RAMS is a media player and management application, with a focus on supporting a single shared media library and
decentralized player architecture.

Currently RAMS has three main pieces: The metadata server, the player daemon, and the UI.

The version of this app stored in the repository is currently a work in progress. Everything compiles, but peripheral
utilities are missing and there may be API incompatibilities between tools.

TODO:
-------------------------------------------------------------------------------------------------------------------------

* Provide a lower-level interface for interaction with the server designed for remote backup and restore, as well as
  adding new stuff.
* Write a new media importer!
* Make sure all the license stuff for the third-part JS libs are properly sorted out.
* Have the player track what tracks are missing info, and send the DB the missing info where possible.
* HTML UI:
	* Allow dragging the progress thumb to seek the track.
* More polish on the player would be nice, some parts are just ugly and poorly written.
