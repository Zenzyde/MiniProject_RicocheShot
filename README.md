# MiniProject_RicocheShot
A tiny game based on the concept of shooting a projectile and having it bounce between pillars in order to gather a highscore. It's a simple practice project for the simple purpose of using the Poisson Disc Sampling algorithm in a game.

Visual example of the miniproject-game in action:
![Shooting-deflection game visual](/images/miniproject_ricochetshot_game_example.png)
![Shooting-deflection game visual editor](/images/miniproject_ricochetshot_game_example_editor.png)
* Points are displayed at the top right, based on how many of the pillars have been destroyed (multiplied by the amount of pillars that were destroyed)
  * If 3 pillars were destroyed, the resulting score added would be 3 * 3 = 9 points. This is to encourage a higher amount of ricochéing shots
* Below the points is the time that has passed, which counts down once the target has been found and shot
  * Once the target has been shot, the time that passed is subtracted from the points as a penalty to encourage fast-paced playing
* There's a target-camera in the bottom right which acts as a guide to make finding the target easier

The shot is visualized with a red "lazer" which switches colour to a goldish yellow when it detects the target:
![Target found example](/images/miniproject_ricochetshot_game_example_target_found.png)
![Target found example editor](/images/miniproject_ricochetshot_game_example_target_found_editor.png)
