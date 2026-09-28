# PickAndPlaceRobot — UC 5: Bin Picking and Sorting

Course project for **61CSE326** (Vietnamese-German University), supervised by Dr.-Ing. Quang Huan Dong.

A Niryo One robot arm in Unity picks a user-selected object out of a bin and places it into the
matching sorting container. Motion planning is done by **MoveIt** in ROS; Unity simulates the
physics and executes the planned trajectories.

The project is based on the
[Unity Robotics Hub Pick-and-Place tutorial](https://github.com/Unity-Technologies/Unity-Robotics-Hub/tree/main/tutorials/pick_and_place)
(Apache-2.0). The ROS side is unchanged from the tutorial; the adaptation to UC 5 is in the Unity
scene and in the scripts listed under "What we added".

---

## Repository structure

```
PickAndPlaceProject/   Unity project (Unity 2020.3.11f1)
  Assets/Scenes/       EmptyScene (tutorial), UC5Scene (our use case)
  Assets/Scripts/      TrajectoryPlanner.cs, ObjectSelector.cs, BinRandomizer.cs
ROS/                   catkin workspace (niryo_moveit, moveit_msgs, ros_tcp_endpoint, ...)
docker/                Dockerfile for the ROS side
```

## Requirements

- Ubuntu (tested on 22.04)
- Unity Editor **2020.3.11f1**
- Docker
- Git

---

## Setup

### 1. Clone

```bash
git clone https://github.com/LamTranNguyen/PickandPlacerobot.git
cd PickandPlacerobot
```

### 2. ROS side (Docker)

Build the image once:

```bash
docker build -t unity-robotics:pick-and-place -f docker/Dockerfile .
```

Then start the container every time you want to run the project:

```bash
docker run -it --rm -p 10000:10000 unity-robotics:pick-and-place /bin/bash
```

Inside the container:

```bash
source devel/setup.bash
roslaunch niryo_moveit part_3.launch
```

Wait until the last lines are `You can start planning now!` and `Ready to plan`.
Leave this terminal running.

### 3. Unity side

1. Open `PickAndPlaceProject` with Unity Hub (Unity 2020.3.11f1).
2. Open the scene `Assets/Scenes/UC5Scene`.
3. Check **Robotics > ROS Settings**: ROS IP Address `127.0.0.1`, Host Port `10000`.
4. Press **Play**. The ROS IP indicator in the top left of the Game view turns green when connected.

---

## How to run the demo

1. Press **Reset** to place the three objects at new random positions inside the bin.
2. Choose a colour (Red / Blue / Green) in the dropdown.
3. Press **Publish**. The robot picks the selected object and places it into the matching container.

---

## What we added on top of the tutorial

| File / object | Purpose |
|---|---|
| `UC5Scene` | Bin with three coloured objects and three matching sorting containers |
| `ObjectSelector.cs` | Dropdown to choose which object to pick; assigns the object and its container to the trajectory planner at run time |
| `BinRandomizer.cs` | Reset button; divides the bin into one cell per object and places each object randomly inside its own cell |
| `TrajectoryPlanner.cs` | Added the public method `SetTargets()` so the target can be changed from another script |

Everything else (MoveIt, `mover.py`, ROS-TCP-Endpoint, launch files) is unchanged from the tutorial.

---

## Known issues

- **No obstacle avoidance yet.** MoveIt only knows the robot model, so the bin, the containers and
  the other objects are invisible to the planner. The bin walls are kept low to avoid collisions.
  This is the subject of Milestone 2.
- **Limited workspace.** The reach of the Niryo One is small. The randomization range in
  `BinRandomizer` is tuned to stay inside the reachable area; widening it causes
  `No trajectory returned from MoverService`.
- **Grasping relies on physics.** Objects are held by friction between the gripper fingers, so an
  inaccurate approach can displace an object instead of grasping it.

## Troubleshooting

- **`Connection refused` in Unity** — the ROS container is not running, or it was started without
  `-p 10000:10000`.
- **"Import Robot from URDF" missing from the context menu (Ubuntu 22.04)** — Unity 2020.3 needs
  `libssl1.1`, which Ubuntu 22.04 does not ship. Install it from the Ubuntu 20.04 archive, delete the
  `Library/` folder and reopen the project.
- **Docker build fails with a GPG "invalid signature" error** — usually caused by low disk space.
  Free space and rebuild.

---

## Team

| Name | Role |
|---|---|
| [Name 1] | [role] |
| [Name 2] | [role] |
| [Name 3] | [role] |
| [Name 4] | [role] |
