# Point cloud spatial renderer

This repository contains the Unity-based framework used for the subjective experiment from [1], and consists of two scenes that can be used to run the experiment on distinct visualization devices. *ELFDScene* and *FlatMonitorScene* are used to conduct the subjective experiment on the Sony's Spatial Reality Display and on a standard flat monitor, respectively. 

Please follow these pre configuration steps prior to running the code: 

* In order to setup the Spatial Reality Display, follow the instructions from this [link](https://www.sony.net/Products/Developer-Spatial-Reality-display/en/develop/Setup/SetupSRDisplay.html). Then, install the Runtime following the instructions from this [link](https://www.sony.net/Products/Developer-Spatial-Reality-display/en/develop/Setup/SetupSRRuntime.html)
* Download and generate the [Spatial Rendering Point Cloud Dataset](https://www.epfl.ch/labs/mmspg/downloads/sr-pcd/)
* Place the following generated folders under *Assets/Dataset*: *distorted*, *original*, *dummies* and *training*
* Open the **Point cloud spatial renderer** project in Unity, open the desired scene and build it

The point size of all the point clouds can be configured in the csv files contained in *Assets/Dataset/point_sizes*. The script *ManageApp.cs* under *Assets/Scripts* controls the experiment and can be edited for modifying the order of the steps in the experiment. After conducting the subjective experiment, the subject identifiers and their scores will be stored under *Assets/Logs*. 

This framework was developed and tested using Unity 2019.4. 

## Dependencies

This project uses the [Pcx tool](https://github.com/keijiro/Pcx) for importing and rendering point clouds in Unity. The [SRDisplay UnityPlugin](https://www.sony.net/Products/Developer-Spatial-Reality-display/en/develop/Unity/Setup.html) is used for rendering the point cloud models in the Spatial Reality Display. These external tools are already included in this repository. 

## Conditions of use

Permission is hereby granted, without written agreement and without license or royalty fees, to use, copy, modify, and distribute the data provided and its documentation for research purpose only. The data provided may not be commercially distributed. In no event shall the Ecole Polytechnique Fédérale de Lausanne (EPFL) be liable to any party for direct, indirect, special, incidental, or consequential damages arising out of the use of the data and its documentation. The Ecole Polytechnique Fédérale de Lausanne (EPFL) specifically disclaims any warranties. The data provided hereunder is on an “as is” basis and the Ecole Polytechnique Fédérale de Lausanne (EPFL) has no obligation to provide maintenance, support, updates, enhancements, or modifications.

If you wish to use the provided script in your research, we kindly ask you to cite [1].

## References

[1] Lazzarotto, Davi, Michela Testolina, and Touradj Ebrahimi. "On the impact of spatial rendering on point cloud subjective visual quality assessment." *2022 14th International Conference on Quality of Multimedia Experience (QoMEX)*. IEEE, 2022.

#### Contact

In case of questions, feel free to contact the following email address: 

davi.nachtigalllazzarotto@epfl.ch