EvolutionSim
============

A simulation of evolutionary effects using pixels as organisms, utilizing logic based on the Hardy-Weinberg model.

----------------------------------------------------------------------


##Premise
When running the screen shows a square of varied colored pixels buzzing about. 
These pixels are little “organisms” (kind of like digital organisms) that are mating and creating 
a new population of organisms 30 times a second, or so. 

##Representation of DNA and genes
Each pixel has a digital DNA made of genes that give varying intensities the 4 colors Red, Green, Blue, Alpha(Transparency).   
For example:  
100,  0,  0,100 are the genes for pure red  
100,100,  0,100 is Fuchsia (bright purple)   
100,100,100,100 is white  
100,100,100, 50 is grey, but only because it is half transparent white, and the background is black.  
  0,  0,  0,  0 is dead (the only special color, also it is black)  
139, 69, 19,100 is brown  
By combination, these genes can form all colors known to a computer screen.  

##Messing with Equilibrium
At the start, they are in equilibrium according to the Hardy-Weinberg equations. Using the controls below, you can adjust  
1. migration  
2. random mating  
3. population size  
4. mutation  
5. and natural selection.  

----------------

#Controls 
(numbers represent the number key at the top of the keyboard)  
1 – Press (or Press and hold) to add random pixels to the environment (migration)  
2 – Press to Toggle Random Mating / Nearest Neighbor Mating  
3 – Catastrophe button (kills all but the top left 3x3 pixels)  
Enter – reset the simulation to a random population.  

The next two have text input.   
Press button, type text, press button again.  
When entering text, the menu in the top left shows what has been entered so far.  
Press button twice (as in not entering text in-between) to turn feature off  

M – mutation chance: adds a chance of mutation to generated pixels  
Example: (Press M) (type) 0.001 (press M) to represent a 0.1% chance of mutation  

N – kill Selection: kills off pixels that are close to the color entered. The closer the color, higher the chance of dying.
Example: (Press N) (type) 100, 0, 0, 100 (Press N)  
These numbers are in Red, Green, Blue, Alpha format, so this example says that any pixels that are close to pure red are likely to die.

Fun things to do:  
Press 2 to toggle to nearest neighbor mating. (looks really cool)  
Press 3 to kill of most of them.  
Watch them repopulate in a stratified fashion.  
(represents general population regrowth after disaster)  

Press 3 like twenty-five times until it is one solid color.  
On the left side, the color of the top left pixel is displayed  
Press N, enter that color, Press N again  
Watch all pixels die off  
(problem with cheetahs’ being non-diverse from overhunting)  
