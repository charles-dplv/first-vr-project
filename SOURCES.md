# Sources

## Impossible space (RoomManager, RoomDetector, zone de RoomGenerator)

- Suma, Lipps, Finkelstein, Krum, Bolas (2012). Impossible Spaces: Maximizing Natural Walking in Virtual Environments with Self-Overlapping Architecture. IEEE TVCG 18(4) : principe de la pièce qui change pendant qu'on fait le tour.
  https://doi.org/10.1109/TVCG.2012.47
- Suma, Clark, Krum, Finkelstein, Bolas, Wartell (2011). Leveraging Change Blindness for Redirection in Virtual Environments. IEEE VR : le changement doit se faire hors du champ de vision (d'où les murs de 3 m et la zone invisible depuis le détecteur).
  https://doi.org/10.1109/VR.2011.5759455
- Vasylevska, Kaufmann, Bolas, Suma (2013). Flexible Spaces: Dynamic Layout Generation for Infinite Walking in Virtual Environments. IEEE 3DUI : génération procédurale des pièces dans un espace qui se recouvre.
  https://doi.org/10.1109/3DUI.2013.6550194

## Génération des pièces (RoomGenerator, PoissonDisk)

- Bridson (2007). Fast Poisson Disk Sampling in Arbitrary Dimensions. SIGGRAPH Sketches : l'algorithme de PoissonDisk.cs (grille de côté r/√2, anneau entre r et 2r, k = 30 essais).
  https://www.cs.ubc.ca/~rbridson/docs/bridson-siggraph07-poissondisk.pdf
- Unity, Random.InitState : même graine = même suite de nombres = même pièce.
  https://docs.unity3d.com/ScriptReference/Random.InitState.html

## Objets en cubes (CubeCutter, prefabs de Assets/Prefabs/RoomObjects)

- Unity, Prefabs : chaque objet est un prefab fait de quelques grands cubes étirés (plateau, pieds, etc.).
  https://docs.unity3d.com/Manual/Prefabs.html
- Voxel : découper un volume en une grille régulière de petits cubes, le principe de CubeCutter.
  https://en.wikipedia.org/wiki/Voxel
- Unity, GameObject.CreatePrimitive : chaque cube (MeshFilter, MeshRenderer, BoxCollider).
  https://docs.unity3d.com/ScriptReference/GameObject.CreatePrimitive.html
- Unity, Object.Instantiate (avec un parent) : le prefab est copié directement dans la pièce, encore inactive.
  https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
- Unity, MonoBehaviour.Awake : Awake n'est appelé qu'à l'activation de l'objet, ce qui laisse le temps de le découper avant que Breakable et XRI lisent ses colliders.
  https://docs.unity3d.com/ScriptReference/MonoBehaviour.Awake.html
- Unity, Component.GetComponentsInChildren (includeInactive) : retrouver les grands cubes d'un objet inactif.
  https://docs.unity3d.com/ScriptReference/Component.GetComponentsInChildren.html
- Unity, Object.Destroy et Object.DestroyImmediate : Destroy attend la fin de la frame, or la pièce peut s'afficher dans la même frame.
  https://docs.unity3d.com/ScriptReference/Object.Destroy.html
  https://docs.unity3d.com/ScriptReference/Object.DestroyImmediate.html
- Vu en test : une pile avec une petite caisse sous deux grandes s'est effondrée toute seule, d'où une seule taille de caisse par pile.

## Casse (Breakable)

- Unity, Collision.relativeVelocity : vitesse du choc, projetée sur la normale du contact (seuil de vitesse).
  https://docs.unity3d.com/ScriptReference/Collision-relativeVelocity.html
- Impulsion d'un choc J = (1 + e) v / (1/m1 + 1/m2), avec e = 0 : J = μ v, μ = m1 m2 / (m1 + m2) la masse réduite (seuil d'impulsion).
  https://en.wikipedia.org/wiki/Collision_response
  https://en.wikipedia.org/wiki/Reduced_mass
- Unity, Collision.impulse et CCD spéculative : pourquoi on ne lit pas collision.impulse (vu en test : 0 pour chaque cube quand un vase lancé à 8 m/s est arrêté par un mur, l'impulsion est aussi répartie entre les colliders d'un objet composé).
  https://docs.unity3d.com/ScriptReference/Collision-impulse.html
  https://docs.unity3d.com/Manual/speculative-ccd.html
- Unity, Rigidbody.GetPointVelocity : vitesse d'un cube détaché (rotation de l'objet comprise).
  https://docs.unity3d.com/ScriptReference/Rigidbody.GetPointVelocity.html
- Ericson (2005). Real-Time Collision Detection. Morgan Kaufmann, §4.2 (AABB) : test de chevauchement de deux boîtes alignées (AreTouching).
- Cormen, Leiserson, Rivest, Stein. Introduction to Algorithms, chapitre Breadth-first search : parcours en largeur de FindParts (flood fill sur les cubes).
- Unity, Mesh.CombineMeshes : un seul mesh par objet intact.
  https://docs.unity3d.com/ScriptReference/Mesh.CombineMeshes.html
- Unity, SRP Batcher : pourquoi le nombre de draw calls compte sur Quest.
  https://docs.unity3d.com/Manual/SRPBatcher.html

## Interaction VR (prefabs de RoomObjects, RoomManager, prefab Bat)

- Unity, XR Interaction Toolkit 3.3, XR Grab Interactable : Movement Type (Velocity Tracking), Attach Transform, Use Dynamic Attach, Retain Transform Parent.
  https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/xr-grab-interactable.html
- Unity, XRBaseInteractable (API) : liste colliders lue à l'enregistrement.
  https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/api/UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.html
- Unity, Interaction between collider types (matrice des collisions) : un objet kinematic contre un collider statique ne déclenche pas OnCollisionEnter, d'où Velocity Tracking.
  https://docs.unity3d.com/Manual/collider-types-interaction.html
- Code source du package XR Interaction Toolkit 3.3.2 (Library/PackageCache/com.unity.xr.interaction.toolkit@*/Runtime/Interaction) :
  XRBaseInteractable.cs, Awake() : si la liste colliders est vide, elle est remplie avec les colliders des enfants (raison pour laquelle on découpe l'objet avant son Awake).
  XRGrabInteractable.cs, Grab() et Drop() : l'objet tenu est sorti de son parent, puis remis dedans seulement si ce parent est actif.
  XRInteractionManager.cs, RegisterInteractable() : la table collider → objet n'est remplie qu'à l'enregistrement (raison pour laquelle un objet attrapable éclate en entier).

## Mode clavier / souris (DesktopPlayer)

- Unity, XRSettings.isDeviceActive : savoir au lancement si un casque est actif (sinon on remplace le rig XR par le joueur clavier).
  https://docs.unity3d.com/ScriptReference/XR.XRSettings-isDeviceActive.html
- Input System, Keyboard : les touches sont repérées par leur place sur le clavier (W A S D en QWERTY = Z Q S D en AZERTY).
  https://docs.unity3d.com/Packages/com.unity.inputsystem@1.11/manual/Keyboard.html
- Unity, CharacterController.SimpleMove (gravité incluse) et minMoveDistance (un déplacement plus petit est ignoré).
  https://docs.unity3d.com/ScriptReference/CharacterController.SimpleMove.html
  https://docs.unity3d.com/ScriptReference/CharacterController-minMoveDistance.html
- Unity, Physics.SphereCast : un rayon épais pour prendre un objet (un rayon fin passe entre deux cubes, vu en test).
  https://docs.unity3d.com/ScriptReference/Physics.SphereCast.html
- Unity, Physics.IgnoreCollision : l'objet tenu ne touche pas le corps du joueur.
  https://docs.unity3d.com/ScriptReference/Physics.IgnoreCollision.html
- Unity, Rigidbody.maxAngularVelocity : la limite par défaut (7 rad/s) est trop basse pour un coup de batte.
  https://docs.unity3d.com/ScriptReference/Rigidbody-maxAngularVelocity.html

## Déplacement et confort en VR (rig XR Origin de MainScene)

- Unity, CharacterController.minMoveDistance : un déplacement plus petit que cette valeur est ignoré. Vu en test à 2,5 m/s avec 0,005 : 2,50 m/s jusqu'à 500 images/s, puis 0 m/s (2,5 / 500 = 0,005 m par image), d'où la valeur 0.
  https://docs.unity3d.com/ScriptReference/CharacterController-minMoveDistance.html
- Unity, Character Controller, Step Offset : hauteur que le joueur monte d'un coup. Vu en test avec 0,5 : montée de 43 cm en une image contre une caisse. Avec 0,13 : plus de montée sur les meubles testés, mais on passe encore sur un cube détaché.
  https://docs.unity3d.com/Manual/class-CharacterController.html
- Unity, XR Interaction Toolkit 3.3, Continuous Turn Provider : rotation continue au stick droit (Turn Speed = 60 °/s, Enable Turn Around désactivé pour éviter le demi-tour instantané).
  https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/continuous-turn-provider.html
- Unity, XR Interaction Toolkit 3.3, Tunneling Vignette Controller : prefab TunnelingVignette placé sous la caméra, déclenché par les providers de déplacement et de rotation.
  https://docs.unity3d.com/Packages/com.unity.xr.interaction.toolkit@3.3/manual/tunneling-vignette-controller.html
- Fernandes, Feiner (2016). Combating VR Sickness through Subtle Dynamic Field-of-View Modification. IEEE 3DUI, p. 201-210 : réduire le champ de vision pendant le mouvement diminue le mal des transports virtuel.
  https://doi.org/10.1109/3DUI.2016.7460053
- Code source du package XR Interaction Toolkit 3.3.2 (Library/PackageCache/com.unity.xr.interaction.toolkit@*/Runtime/Inputs/Simulation/XRInteractionSimulator.cs), ProcessControlInput() : le stick gauche n'est mis à jour que si Shift est tenu, donc il reste poussé si Shift est relâché avant la lettre (vu en test : 2,50 m/s sans aucune touche).
