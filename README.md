# Screen Space Wetness

Screen Space Wetness is a rendering effect for Unity HDRP that dynamically applies a wetness (rain puddle) effect across the entire screen.

![alt text](image/preview.png)

## About
Screen Space Wetness is a rendering effect for Unity HDRP that dynamically applies a wetness (rain puddle) effect across the entire screen. It modifies the normal and smoothness buffers without the need to create custom shaders for each material and object. The effect also supports custom shadow maps and mask buffers, allowing you to mask out specific layers and exclude areas occluded by other objects, such as regions under a roof.

## Installation
### Manual
1. Clone the repository:
    ```sh
    git clone https://github.com/misakieku/screen-space-wetness.git
    ```
2. Open your Unity project and import the cloned repository.
### Package Manager
1. Go to Window->Package Manager, click on the + sign, and Add from git: https://github.com/misakieku/screen-space-wetness.git

## Features
- Full-Screen Wetness Effect: Applies a rain puddle effect across the entire screen.
- Dynamic Buffer Modification: Changes normal and smoothness buffers without custom shaders.
- Custom Shadow Map Support: Handles occlusions with custom shadow maps.
- Mask Buffer Support: Masks out specific layers to control the wetness effect.