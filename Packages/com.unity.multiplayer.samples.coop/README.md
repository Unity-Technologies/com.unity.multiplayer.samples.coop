# TODOs (trimmed down version of tasks listed at: https://github.cds.internal.unity3d.com/unity/com.unity.template-starter-kit)

##### Fill in your project template's package information

	Update the following required fields in `Packages/com.unity.template.mytemplate/package.json`:
	- `name`: Project template's package name, it should follow this naming convention: `com.unity.template.[your-template-name]`
    (Example: `com.unity.template.3d`)
	- `displayName`: Package user friendly display name. (Example: `"First person shooter"`). <br>__Note:__ Use a display name that will help users understand what your project template is intended for.
	- `version`: Package version `X.Y.Z`, your project **must** adhere to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).
	- `unity`: Minimum Unity Version your project template is compatible with. (Example: `2018.3`)
	- `description`: This is the description for your template which will be displayed to the user to let them know what this template is for. This description shouldn't include anything version-specific and should stay pretty consistent across template versions.
	- `dependencies`: Specify the dependencies the template requires. If you add a package to your project, you should also add it here. We try to keep this list as lean as possible to avoid conflicts as much as possible.

##### Update **README.md**

    The README.md file should contain all pertinent information for template developers, such as:
	* Prerequisites
	* External tools or development libraries
	* Required installed Software

The Readme file at the root of the project should be the same as the one found in the template package folder. 

##### Prepare your documentation

    Rename and update **Packages/com.unity.template.mytemplate/Documentation~/your-package-name.md** documentation file.

    Use this documentation template to create preliminary, high-level documentation for the _development_ of your template's package. This document is meant to introduce other developers to the features and sample files included in your project template.

    Your template's documentation will be made available online and in the editor during publishing to guide our users.

##### Update the changelog   

    **Packages/com.unity.template.mytemplate/CHANGELOG.md**.

	Every new feature or bug fix should have a trace in this file. For more details on the chosen changelog format, see [Keep a Changelog](http://keepachangelog.com/en/1.0.0/).

	Changelogs will be made available online to inform users about the changes they can expect when downloading a project template. As a consequence, the changelog content should be customer friendly and present clear, meaningful information.

#### Complete the rest of the steps in the link regarding Legal & Testing