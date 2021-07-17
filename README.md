# PathOS

## PathOS+
The next iteration of this project is known as "PathOS+". It updates the project so that it can support expert evaluation. This new version is available at: https://github.com/AtiyaNova/pathosplus

## About

PathOS is a project exploring the potential of AI agents to stand in for human players in early-stage level testing for games with 3D navigation in a virtual world. 

STATUS: Polish and testing of initial prototype

#### Team

Samantha Stahlke - Lead Developer

Atiya Nova - Research Assistant, Developer

Dr. Pejman Mirza-Babaei - Research Supervisor

### Using PathOS

PathOS is being developed as an open-source framework for [Unity](https://unity.com/). To use PathOS in your project, all you have to do is create some simple markup highlighting interactive objects in your level, and instantiate AI agents to wander around. PathOS operates on top of Unity's Navmesh system, and requires no modification to your existing game objects or scripts. Here's a screenshot of the framework in action:

![Screenshot of PathOS Runtime UI][screenshot_ui]

Agents can be customized to reflect different player profiles - for instance, cautious newbies focused on exploration, hardcore completionists, or diehard adrenaline junkies looking for a fight. Agents will navigate based on these profiles, giving you an approximation of how different players will navigate through your game's world.

You can find the manual for PathOS [here](https://drive.google.com/open?id=1Q19IY_Xm924RNgSqcFsv3I-s80j7yL7W).

[screenshot_ui]: https://i.imgur.com/CqAFg4l.png "PathOS Runtime UI"
