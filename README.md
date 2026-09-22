# Line Picture Host (LPH)
It's a light-weight and fast working GDI+ based vector drawing program. No premium, no payments, no "cloud integrations" it just works.

### Setup process
It's very straight forward, just open the program, press "Create new LPH file" and draw.

### Opening on the network
1) Press "Open host to the network"
2) In the opened window, press "Open this host to the network"
3) Network Port: 0.0.0.0:1300
4) Press Connect button
5) Go to the first window and press "Create new LPH file"
6) Press "Serial Port Options"
7) Enter IP-address and port you selected, if you're testing on your PC, just type 127.0.0.1:1300
8) Press Connect button
9) Draw something in the Editor, and it's gonna appear in the LPH Viewer, after you press the Force Redraw button on Viewer's side
10) Enjoy!

### Using tetronet
1) If you are in the same LAN, don't draw through the tetronet. It will add delay, and can be overloaded
2) Create file "tetronet.txt" and fill it like that:
   ```
   tetronet
   virtual
   wss://data-set.su:3000/
   socket.io
   ```
   P.s. if you want speed make your own FCIAS (repository is available) and use ws://yourfcias.net:30000/ and swap "socket.io" to "websocket"
4) Follow the TCP socket instruction, but instead of typing 0.0.0.0:130, leave it empty and press "Tetronet Address"
5) And on the LPH Editor's side, type in the address of the viewer. It's gonna be displayed as a pop-up window, and in the title of the viewer, so you won't forget it. Also press the "Tetronet Address" mark
6) Enjoy!

### Moments
Viewer will send cursor movement to the editor, but for some reason until you press Force Redraw in the editor, the cursor is not erasing after a movement, so just press it once. Also tetronet can be laggy, so if you don't need NAT traversal because you're drawing between 2 computers in the same LAN, just use TCP sockets.

### Security
LPH doesn't encrypt your data by default, so press the Authentification and Encryption marks, and you must choose a login and a password, which must be the same on the editor and the viewer. Use a strong password, so nobody will hack you. Password is transported in an encrypted form only if you enabled Encryption. So be very careful with that. If you're using tetronet, even through wss, the tetronet server could see your drawing. So also encrypt in this case.

### Storage
Files are not stored on any servers. They're stored locally, until you upload them on to a cloud storage. No personal data is collected, LPH is anonymous.

### Speed
LPH works very fast even on potato computers. I checked it on a computer that runs minecraft at 640x480 at 40 fps, and LPH draws about 60k objects per second.

### Future of the project
I'm not a future predictor, but probably I'm gonna add more features, like applying graphical effects, but there will be only features, which laser monitors can draw. No rectangles, circles, and other 2d objects.
