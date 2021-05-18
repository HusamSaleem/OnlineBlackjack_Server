# Description
- This is the server code for the Online Blackjack app
- Handles connections and game sessions

# Features
- Automatically removes clients from an active client Dictionary/HashMap that have closed the app and don't ping the server back within 3 tries in 60 seconds (You can edit this feature in the ClientPingHandler class)
- A basic matchmaking service where it can be easily scaled to as many players as you want (MatchmakingService Class). However, if you do add more players to a single game session, then you would have to also modify the UI of the clientside app to support the players you want. 
- Each Game Session is essentially running on a different thread
- Uses JSON to send and receieve data as the default way to do so
- Automatically resends the last data sent that has been lost or corrupted on request by the client
- Everything that has to do with the game is done by the server (NEVER TRUST THE CLIENT! :) )

# Setup
- First, import this repository into Microsoft Visual Studio or whatever IDE you use
- Next up, if you want to change the port that your server listens on, then you can do so in the Server Class (Default port is 6479)
- Now you have to port forward with TCP protocol and the port you desire or keep it at the default port (6479). Also make sure to know what IP address you want to host this server. 
- TIP: On most routers, there should be an option to reserve an IP Address so that when you host the server, you won't have to manually change the IP Address of the port forwarding service every time it changes for that computer you are using
- When you have finished the above steps, try to build and compile the server to see if is working as intended.
- The next steps to set up the client are on this repo (https://github.com/HusamSaleem/OnlineBlackjack_Client)
