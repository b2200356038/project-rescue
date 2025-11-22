# Coop Rescue Game
A work-in-progress cooperative multiplayer driving game featuring custom-built vehicle physics and networked gameplay.

## Features
* Custom vehicle physics with realistic suspension, friction, and tire simulation
* Full multiplayer support with Unity Netcode for seamless co-op gameplay
* Character controller with vehicle interaction and seat management
* Voice and text chat via Vivox integration

## Vehicle Physics System
* Dynamic spring–damper suspension system
* Real-time ground detection & contact handling
* Multi-substep friction solver for accurate results
* Motor torque, braking, and steering systems

  
![ezgif-7fd7b0b4265cacfd](https://github.com/user-attachments/assets/878ca689-3507-4dbc-8e6b-1ac31e9ce78c)



## Multiplayer Architecture
* Unity Netcode for GameObjects
* Distributed authority model using Unity Multiplayer Services
* Efficient wheel-state synchronization with adaptive update rates
* Physics replication optimized for network performance

## Technologies Used
* Unity 6
* Unity Netcode for GameObjects
* Unity Multiplayer Services
* Vivox (Voice/Text Chat)
* Modular architecture using assembly definitions
