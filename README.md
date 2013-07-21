Ex Libris Rosetta Update AIP App
================================

Introduction
------------

This application interacts with the Ex Libris Rosetta Digital Repository to add, remove, and replace individual files in an existing AIP (Archival Information Package).

Usage
-----
	C:\>UpdateAIP.exe
 
	Update AIP can be used to replace or delete a file in an existing AIP stored in Rosetta. It uses Rosetta APIs to create a new revision of the representation which contains the file.
	UpdateAIP [-replace|-delete] [IEPID] [FLPID] [config options] file
 	
	Parameters:
        	action:     replace (default) or delete
        	PID:        PID of IE (IEXXXX) and/or file (FLXXXX) (optional)
        	file:       Path to the local file to replace. Needs to be the same name as the file to be replaced/deleted.
 	
	Configuration Parameters (saved between sessions):
        	user|u:     Server username for scp
        	pass|p:     Server password for scp
        	host|h:     Server host
        	port:       Server application port (default 1801)
        	appuser|au: Application username for web services
        	apppass|ap: Application password for web services
        	remotedir:  Directory on remote server to place file (default /tmp/updateaip)
        	inst:       Application institution for web services (default INS00)

About the App
-------------
The application is written in C# and uses the Rosetta APIs to interact with system.