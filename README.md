# Rage room VR

**Le projet** : une rage room dans un espace impossible. On casse les objets de la pièce (à la main, en les lançant ou avec la batte), puis on fait le tour des murs du milieu pour passer dans une nouvelle pièce, alors qu'on reste en fait toujours au même endroit. Si on revient en arrière on retrouve l'ancienne pièce, toujours cassée.

**Espace impossible** : un trigger (RoomDetector) change le contenu de la pièce pendant que le joueur est derrière les murs et ne la voit pas. RoomManager garde les pièces déjà vues dans un dictionnaire.

**Génération** : chaque pièce est générée avec une graine (son numéro), les meubles sont placés avec un Poisson disk sampling (Bridson) pour qu'ils ne se chevauchent pas. Les objets sont des prefabs faits de quelques cubes étirés.

**Casse** : au chargement, CubeCutter découpe chaque prefab en petits cubes. Breakable regarde la vitesse et l'impulsion du choc (J = μv), si ça dépasse un seuil les cubes touchés se détachent, et un parcours en largeur sépare l'objet en morceaux. Le nombre de cubes par pièce est limité (environ 1100) pour tenir sur le Quest.

**Sans casque** : seulement dans Unity, pas dans le build. Si on active le GameObject DesktopPlayer dans MainScene, il remplace le rig XR et on joue au clavier et à la souris. ZQSD + souris, clic gauche pour prendre / lancer / frapper, clic droit pour lâcher, Échap pour libérer la souris.

**Ce qu'il aurait fallu faire ensuite** : pour l'instant un objet casse pareil partout, une table tapée sur le plateau ou sur un pied réagit de la même façon. Il faudrait que la casse dépende de l'endroit touché, par exemple un pied qui casse plus facilement et fait tomber le plateau. Il faudrait aussi des matériaux (bois, verre, métal...) avec chacun leur résistance, au lieu de seuils réglés objet par objet. Enfin tous les prefabs sont volontairement très cubiques. Il faudrait tester avec des objets ronds (vase, ballon...) et, selon le résultat, adapter CubeCutter pour qu'il découpe aussi des formes rondes.

## Sources

- Bridson (2007). Fast Poisson Disk Sampling in Arbitrary Dimensions. https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
- Impulsion d'un choc et masse réduite : https://en.wikipedia.org/wiki/Collision_response
- XR Interaction Toolkit 3.3 : https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/index.html
