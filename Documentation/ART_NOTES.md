
# **Art Notes**

Hi! Welcome to the art notes, a page that covers some of the art in Boss Room.

## **Baking Overview**

Boss Room is set up to support baked lighting. Please feel free to use the existing setup or modify it as needed. <br /><br />

### **TL:DR**

![LightExplorer](Images/BossRoomMenu.png)

The main Boss Room level is separated into three scenes (Assets > BossRoom > Scenes > BossRoom):
  * Dungeon BossRoom
  * Dungeon Entrance
  * Dungeon Transition

To bake, open the BossRoom scene. Load the pieces of the dungeon listed above into that scene, make sure you're using the baking version of all of the lights, then generate your lighting using the settings you'd like from the Boss Room Lighting Setup menu. <br /><br />

* **To bake all lights:**
  * Go to menu Boss Room > Lighting Setup > All Baked
  * Go to lighting settings (Window > Rendering > Lighting). Use the FastBaking baking profile & hit Generate Lighting button to bake all lights<br /><br />

* **To use only realtime lighting (except area lights):**
  * Go to menu Boss Room > Lighting Setup > All Realtime (except area lights)
  * Go to lighting settings (Window > Rendering > Lighting). Use the FastBaking baking profile & hit Generate Lighting button to lava bake area lights lights<br /><br />

* In your lighting settings, use the **FastBaking baking profile for a fast, but lower quality bake**. Use the **GoodBaking baking profile for a slow but higher quality bake** <br /><br />

* **To clear all baked data (including baked area lights)**
  * Go go lighting settings (Window > Rendering > Lighting). Click on dropdown arrow on Generate Lighting button, then select Clear Baked Data from the menu

    ![AutoGenerate](Images/ClearBakingData.png) <br /><br />

### **TL:DR+**
* Other lighting options (such as relfection probes and light probes) can be set to baked / realtime or enabled / disabled in this menu

* Lights used for baking will be brighter than the realtime ones. This is to compensate for them becoming darker when they bake down

* By default, area lights (used for the glow around the lava) are baked in Boss Room. Area lights can only be baked, not realtime, in Unity currently <br /><br />

### **Baked Lighting & Navigating Settings:**
* The light explorer window can be found at Window > Rendering > Light Explorer. This window can be used to see all of the lights in your scene(s), plus all of their settings <br />

    ![LightExplorer](Images/LightExplorer.png) <br /><br />

* Each light is set to one of three modes: 
  
  * Realtime - not baked, will contribute to realtime lighting, will not contribute to lightmaps
  * Mixed - baked and realtime, will contribute to realtime lighting, will contribute to lightmaps
  * Baked - not realtime, will not contribute to realtime, will only contribute to lightmaps
  
    ![LightMode](Images/LightMode.png) <br /><br />

### **Lighting Settings**

Lighting settings can be found at Window > Rendering > Lighting

![LightingSettings](Images/LightingPanel.png)

* There are three tabs at the top for navigating your lighting options:
  * Scene: light baking options for the current scene(s)
  * Environment: settings for overall environment changes (ambient color, skybox, etc.) in your current scene(s)
  * Baked Lightmaps: where you can inspect your baked light maps if you have baked lighting for your current scene(s) <br /><br />
  
> **⚠ Important ⚠**
  >* The only baking settings that affect **performance of your game** are:
    >* Lightmap resolution
    >* Lightmap padding
    >* Max Lightmap Size
    >* The Compress Lightmaps checkbox
    >* The Directional mode 
    >
        >![ImportantLightingSettings](Images/ImportantLightingSettings.png)
  >* All other settings will only affect the **quality of the bake** 
  >* Higher values in your settings will make baking take longer
    >* It's recommended to start with very low values for your settings to make baking fast and lighting iteration easy. Then, when you have something you like, you can bump up these settings for a slower, but better quality bake 
    >* For example, start with a low Lightmap Resolution, like 1, for iteration. Then, bump it up to 6 or 8 for a higher quality bake 

 <br />       
    
* You can hover over each setting and a tooltip will describe what it does <br /><br />

* Directional Mode toggles to whether or not normal maps will influence your baked lighting. Changing your settings to Directional will generate a better quality bake, but is more costly. When baking, additional textures, called directional maps, will be generated for every baked lightmap texture. For example, if you have three lightmaps, you will have three directional maps.  <br /><br />

* In the lighting panel, there is a checkbox that allows Unity to auto-generate lighting whenever your lighting changes, a model is moved, a lighting setting is changed, etc.. This can be useful for quick lighting iteration
 
  ![AutoGenerate](Images/AutoGenerateLighting.png) <br /><br />

* Multiple scenes can be baked at once by loading them together (click and drag scenes into the heirarchy), and generating lighting. Overlapping geometry in your scenes scenes can cause baking artifacts though, so only bake them together if they're not overlapping <br /><br />

* Meshes must be set to contribute to baked Global Illumination to receive baked lighting 
  
  ![MeshSettings](Images/MeshSettings.png) <br /><br />

* Reflection probes can also be baked to save performance <br /><br />




## **Resources from Unity for more details:**
  * [How to build Lightmaps in Unity 2020.1 | Tutorial](https://youtu.be/KJ4fl-KBDR8) (this is a short overview video on baking)
  * [Harnessing Light with URP and the GPU Lightmapper | Unite Now 2020](https://youtu.be/hMnetI4-dNY) (this video is quite long, but runs through everything you'd want to know about baking, light probes, UVs, etc. Check out the comments for more tips and tricks, too)
  * [Unity Manual: Lightmappers](https://docs.unity3d.com/Manual/Lightmappers.html)
  * [Unity Manual: Light Modes](https://docs.unity3d.com/Manual/LightModes.html)
  * [Unity Manual: Light Probes](https://docs.unity3d.com/Manual/LightProbes.html)
  * [Unity Manual: Lighting Window](https://docs.unity3d.com/Manual/lighting-window.html)
  * [Unity Manual: Lighting Settings](https://docs.unity3d.com/Manual/class-LightingSettings.html)
  * [Unity Manual: Lighting Explorer](https://docs.unity3d.com/Manual/LightingExplorer.html)
  * [Unity Manual: Lightmapping Directional](https://docs.unity3d.com/Manual/LightmappingDirectional.html)
  * [Unity Manual: Reflection Probes](https://docs.unity3d.com/Manual/class-ReflectionProbe.html)