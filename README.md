# Placenote SDK Unity Sample app.
The Placenote Software development kit (SDK) allows developers to create mobile applications that are location aware indoors without the need for GPS, markers or beacons. The SDK is compatible with all ARKit enabled phones and can be used to create persistent augmented reality experiences using Unity!
The Placenote SDK Unity Sample app provided here is to serve as an example on how to integrate the SDK into a Unity app. This app is written primarily in C#. Questions? Comments? Issues? Come see us on [Slack](https://placenote.com/slack)

* Note: The Placenote SDK for Unity currently only supports ARKit enabled iOS devices. Android support is planned for mid 2019.

## Getting Started

* First off, you will need to create a developer account and generate an API Key on our website:
  * https://developer.placenote.com/

## How to Download and Install Placenote

### Using the .unitypackage (RECOMMENDED)
* Download the latest Placenote release Unity package from here:
  * [Latest Unity Release](https://github.com/Placenote/PlacenoteSDK-Unity/releases/latest)

* Follow the official documentation to install Placenote and build your first app:
  * [Build a sample Placenote app](https://placenote.com/docs/unity/build-sample-app/)

### Using this Github repository
* If you want to extend this sample project, you can clone this repository but remember that to add Placenote to your own project, the .unitypackage method is highly recommended. Most developers use the above method.

* Before you clone the repo, note that critical library files are stored using lfs, which is the large file storage mechanism for git. You need to install git-lfs before you can clone this repo.
  * To Install these files, install lfs either using HomeBrew:

     ```Shell Session
     brew install git-lfs
     ```

      or MacPorts:
      ```Shell Session
      port install git-lfs
      ```

  * And then, to get the library files, run:
     ```Shell Session
     git lfs install
     git lfs pull
     ```
  * More details can be found on the [git lfs website](https://git-lfs.github.com/)

* After you have this repo, you can continue using the [Placenote official documentation] to build the project on iOS. (https://placenote.com/docs/unity/build-instructions/)
