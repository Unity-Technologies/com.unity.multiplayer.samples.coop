# Photon Setup Guide @Boss Room

## Setup Photon

1. Access Wizard from Window → Photon Realtime → Wizard
2. Click next until you get to the Photon Cloud Setup Part
3. If you don't already have an account or an App Id, click on the Visit Dashboard button in order to create one	
    1.  If you just created an account for the first time you need to also create a New Application from Photon's Dashboard
    2. NOTE! The Photon Type for Boss Room is: `Photon Realtime`
       
   ![Images/Photon-App.png](Images/Photon-App.png)

4. Once an Application is setup, you will be able to get it's App ID from Photon's Dashboard, copy that to your clipboard
    - NOTE! This App Id **must** be shared with other people in order to be able to connect to your Photon Room!

   ![Images/Photon-App.png](Images/Photon-Dashboard.png)

5. Go back to the Photon Wizard Window of your Unity Project and paste the App Id there
6. Click on Setup
    - It should show something like: "Your AppId is now applied to this Project **Done** "
7. Click on Next, then Done, you should be all setup now!
8. You can safely quit the Photon Wizard now!

## Playing Boss Room with friends

Once the setup is done, there are two ways you can actually test out is working:

* Launch the Boss Room project using a packaged build.
* Use the Unity Editor, but do that with caution!

With that being said, both ways are almost the same in terms of how you host or join a session.

```
 IMPORTANT! There is a bug in MLAPI at the moment that could prevent users from connecting to each other through editor!
```

---

NOTE! Just to reiterate, it is very important that all Unity Editor users should have the exact same version of the project with no changes locally and their PhotonAppSettings should match with the Host's one.

---

### Hosting a Room

---
If you want to host a session then:


1. Click the Start button
2. Select `Relay Host` from the dropdown on the left
   ![Images/Boss-Room-Host-Dropdown.png](Images/Boss-Room-Host-Dropdown.png)
   
3. A random generated room name will be assigned.
   ![Images/Boss-Room-Host-Confirm.png](Images/Boss-Room-Host-Confirm.png)
   
4. Share the generated room name with your users, and click confirm!
   
5. Done! You are now in the Lobby - also known as the Character Selection Screen, your friends/users should be able to join know.
   
- NOTE! You can see the Room Name, in the top-left corner!
  ![Images/Boss-Room-Lobby.png](Images/Boss-Room-Lobby.png)

### Joining a Room

---

If you want to Join a session then:
1. Click the Join Button and select `Relay Host` from the dropdown on the left.
   ![Images/Boss-Room-Join-Dropdown.png](Images/Boss-Room-Join-Dropdown.png)
2. You should be asked to input the room name below.
   ![Images/Boss-Room-Join.png](Images/Boss-Room-Join.png)

3. In this example, the room name is: `YQWOWS`

4. Click Join, and once successfully connected, now you should be in the Lobby with the rest of your friends .
   ![Images/Boss-Room-Lobby-Extra.png](Images/Boss-Room-Lobby-Extra.png)
   
