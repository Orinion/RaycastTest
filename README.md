# RaycastTest

This is an example Scene for raycasting using the DepthAPI.
The code is based on a [comment by TudoJude](TudorJude:%20https://github.com/oculus-samples/Unity-DepthAPI/issues/16#issuecomment-1863006589). However the code was not working for me so i changed the compute shader to only compute the depth and use that to calculate the raycast position.
This also makes it [compatible with the BiRP](https://github.com/Orinion/RaycastTest/tree/BiRP).

Example is in `Assests/Scenes/SampleScene`

Main logic in `Assets/Raycast/AutoRayCast.cs`

Controlls on Righthand:

- Trigger = Raycast,
- A = change number of raycasts
- B = change size of scan area (if not single)

Left hand:
- X = delete all cubes
- Y = toggle occlusion material
