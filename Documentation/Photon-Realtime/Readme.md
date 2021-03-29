# Photon Setup Guide for Boss Room

## Setup Photon

1. Access the Wizard from Window → Photon Realtime → Wizard.
2. Click next through the wizard until you get to the Photon Cloud Setup.
3. If you do not already have an account or an App Id, click the Visit Dashboard button to create one. If you created a new account, create a New Application from Photon's Dashboard.
    **Note:** The Photon Type for Boss Room is `Photon Realtime`.
       
   ![Images/Photon-App.png](Images/Photon-App.png)

4. After the Application is setup, you can get its App ID from Photon's Dashboard. Copy the App ID to your clipboard.
    
    **Note:** This App Id **must** be shared with other people in order to be able to connect to your Photon Room.

   ![Images/Photon-App.png](Images/Photon-Dashboard.png)

5. In the Photon Wizard Window of your Unity Project, paste the App Id.
6. Click on Setup. You should receive a completion message, for example "Your AppId is now applied to this Project **Done**".
7. Click Next then Done. Setup is complete and you can safely quit the Photon Wizard.

## Playing Boss Room with friends

Once the setup is done, there are two ways you can actually test out is working:

* Launch the Boss Room project using a packaged build.
* Use the Unity Editor, but do that with caution!

With that being said, both ways are almost the same in terms of how you host or join a session.


> **IMPORTANT!** There is a bug in MLAPI at the moment that could prevent users from connecting to each other through the editor. 

> **Note:** To reiterate, it is important that all Unity Editor users have the exact same version of the project with no changes locally and their PhotonAppSettings should match with the Host's settings.


### Hosting a Room

---
If you want to host a session then:


1. Click the Start button.
2. Select `Relay Host` from the dropdown on the left.
   ![Images/Boss-Room-Host-Dropdown.png](Images/Boss-Room-Host-Dropdown.png)
   
3. A random generated room name will be assigned.
   ![Images/Boss-Room-Host-Confirm.png](Images/Boss-Room-Host-Confirm.png)
   
4. Share the generated room name with your users, and click confirm!
   
5. Done! You are now in the Lobby - also known as the Character Selection Screen. Your friends/users should be able to join now.
   
> **Note:** You can see the Room Name in the top-left corner.
  ![Images/Boss-Room-Lobby.png](Images/Boss-Room-Lobby.png)

### Joining a Room

---

If you want to Join a session then:
1. Click the Join Button and select `Relay Host` from the dropdown on the left.
   ![Images/Boss-Room-Join-Dropdown.png](Images/Boss-Room-Join-Dropdown.png)
2. You should be asked to input the room name below.
   ![Images/Boss-Room-Join.png](Images/Boss-Room-Join.png)

3. In this example, the room name is: `YQWOWS`

4. Click Join. Once successfully connected, you should be in the Lobby with the rest of your friends.
   ![Images/Boss-Room-Lobby-Extra.png](Images/Boss-Room-Lobby-Extra.png)
   
