>>>
**_Project Template Documentation_**

Use this template to create preliminary, high-level documentation meant to introduce users to the feature and the sample files included in this project template. When writing your documentation, do the following:

1. Follow instructions in blockquotes.

2. Replace angle brackets with the appropriate text. For example, replace "&lt;template name&gt;" with the official name of the project template.

3. Delete sections that do not apply to your project template. For example, a template containing only sample files does not have a "Using &lt;template name&gt;" section, so this section can be removed.

4. After documentation is completed, make sure you delete all instructions and examples in blockquotes including this preamble and its title:

		```
		>>>
		Delete all of the text between pairs of blockquote markdown.
		>>>
		```
>>>

# About &lt;template name&gt;

>>>
Name the heading of the first topic after the **displayName** of the project template as it appears in the template's manifest. Check with your Product Manager to ensure that the template is named correctly.

This first topic includes a brief, high-level explanation of the project template and, if applicable, provides links to Unity Manual topics.
>>>

**_Example:_**

>>>
Here is an example for reference only. Do not include this in the final documentation file:

*The First Persion Shooter project template includes examples of First Person Shooter assets, First Person Shooter Instances, animation, GameObjects, game mechanics and scripts that will help you get started quickly with creating your own first person shooter game.*
>>>

<a name="UsingProjectTemplate"></a>
# Using &lt;template name&gt;
>>>

The contents of this section depends on the type of project template.

* At a minimum, this section should include reference documentation that describes the assets, structure, and properties that makes up the project template's content. This reference documentation should include screen grabs (see how to add screens below), a list of assets or settings, an explanation of what each asset or setting does, and the default values of each asset or setting.
* Ideally, this section should also include a workflow: a list of steps that the user can easily follow that demonstrates how to use the project template. This list of steps should include screen grabs (see how to add screens below) to better describe how to use the feature.

For project templates that include sample files, this section may include detailed information on how the user can use these sample files. Workflow diagrams or illustrations could be included if deemed appropriate.

## How to add images

*(This section is for reference. Do not include in the final documentation file)*

If the [Using &lt;template name&gt;](#UsingProjectTemplate) section includes screen grabs or diagrams, a link to the image must be added to this MD file, before or after the paragraph with the instruction or description that references the image. In addition, a caption should be added to the image link that includes the name of the screen or diagram. All images must be PNG files with underscores for spaces. No animated GIFs.

An example is included below:

![A cinematic in the Timeline Editor window.](images/example.png)

Notice that the example screen shot is included in the images folder. All screen grabs and/or diagrams must be added and referenced from the images folder.

For more on the Unity documentation standards for creating and adding screen grabs, see this confluence page: https://confluence.hq.unity3d.com/pages/viewpage.action?pageId=13500715
>>>



# Technical details
## Requirements

>>>
This subtopic includes a bullet list with the compatible versions of Unity. This subtopic may also include additional requirements or recommendations for 3rd party software or hardware. If you need to include references to non-Unity products, make sure you refer to these products correctly and that all references include the proper trademarks (tm or r)
>>>

This version of &lt;template name&gt; is compatible with the following versions of the Unity Editor:

* 2018.3 and later (recommended)

To use this project template, you must have the following 3rd party products:

* &lt;product name and version with trademark or registered trademark.&gt;
* &lt;product name and version with trademark or registered trademark.&gt;
* &lt;product name and version with trademark or registered trademark.&gt;

## Known limitations
>>>
This section lists the known limitations with this version of the project template. If there are no known limitations, or if the limitations are trivial, exclude this section. An example is provided.
>>>

&lt;template name&gt; template version &lt;template version&gt; includes the following known limitations:

* &lt;brief one-line description of first limitation.&gt;
* &lt;brief one-line description of second limitation.&gt;
* &lt;and so on&gt;

>>>
*Example (For reference. Do not include in the final documentation file):*

The First Person Shoot template version 1.0 has the following limitations:*

* The First Person Shooter template does not support sound.
* The First Person Shooter template's Recorder properties are not available in standalone players.
* MP4 encoding is only available on Windows.
>>>

## Project template contents
>>>
This section includes the location of important files you want the user to know about. For example, if this project template containing user interface, models, and materials separated by groups, you may want to provide the folder location of each group.
>>>

The following table indicates the &lt;describe the breakdown you used here&gt;:

|Location|Description|
|---|---|
|`<folder>`|Contains &lt;describe what the folder contains&gt;.|
|`<file>`|Contains &lt;describe what the file represents or implements&gt;.|

>>>
*Example (For reference. Do not include in the final documentation file):*

The following table indicates the root folder of each type of sample in this project template. Each sample's root folder contains its own folders:

|Folder Location|Description|
|---|---|
|`WoodenCrate_Orange`|Root folder containing the assets for the orange crates.|
|`Characters`|Root folder containing the assets and animators for the characters.|
|`Levels`|Root folder containing scenes for the sample game's levels.|
>>>

## Document revision history
>>>
This section includes the revision history of the document. The revision history tracks when a document is created, edited, and updated. If you create or update a document, you must add a new row describing the revision.  The Documentation Team also uses this table to track when a document is edited and its editing level. An example is provided:

|Date|Reason|
|---|---|
|Sept 12, 2017|Unedited. Published to production.|
|Sept 10, 2017|Document updated for project template version 1.1.<br>New features: <li>audio support for capturing MP4s.<li>Instructions on saving Recorder prefabs|
|Sept 5, 2017|Limited edit by Documentation Team. Published to production.|
|Aug 25, 2017|Document created. Matches project template version 1.0.|
>>>
