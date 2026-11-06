# Rage room VR : explication des systèmes

Le projet est une rage room en VR (Unity 6, XR Interaction Toolkit 3.3.2) placée dans un « impossible space ». On fait le tour d'un mur en L. À chaque passage dans le détecteur, le contenu de la pièce change. Si on fait demi-tour, on retrouve la pièce d'avant, telle qu'on l'a laissée (cassée). Tout ce qui est dans la pièce est généré par le code, et tout peut être cassé. On peut prendre des objets, les lancer, et taper avec une batte, au casque ou au clavier.

Les sources de chaque partie sont dans [SOURCES.md](SOURCES.md).

## Sommaire

1. [Vue d'ensemble](#1-vue-densemble)
2. [L'impossible space : RoomDetector et RoomManager](#2-limpossible-space--roomdetector-et-roommanager)
3. [La génération des pièces : RoomGenerator et PoissonDisk](#3-la-génération-des-pièces--roomgenerator-et-poissondisk)
4. [Les objets en cubes : ChunkBuilder et RoomObjects](#4-les-objets-en-cubes--chunkbuilder-et-roomobjects)
5. [La casse : Breakable](#5-la-casse--breakable)
6. [L'interaction VR : XR Interaction Toolkit et la batte](#6-linteraction-vr--xr-interaction-toolkit-et-la-batte)
7. [Le mode clavier : DesktopPlayer](#7-le-mode-clavier--desktopplayer)
8. [Réglages](#8-réglages)
9. [Tests réalisés](#9-tests-réalisés)
10. [Limites connues](#10-limites-connues)

---

## 1. Vue d'ensemble

### Les fichiers

| Fichier | Rôle |
|---|---|
| [RoomDetector.cs](Assets/Scipts/RoomDetector.cs) | Regarde la tête du joueur et dit quand il traverse le détecteur, et dans quel sens |
| [RoomManager.cs](Assets/Scipts/RoomManager.cs) | Garde toutes les pièces, crée la suivante si besoin, affiche celle où on est |
| [RoomGenerator.cs](Assets/Scipts/RoomGenerator.cs) | Remplit une pièce : où mettre les meubles, quoi poser dessus, quelles couleurs |
| [PoissonDisk.cs](Assets/Scipts/PoissonDisk.cs) | Tire des points au hasard qui ne sont jamais trop proches (algorithme de Bridson) |
| [RoomObjects.cs](Assets/Scipts/RoomObjects.cs) | Catalogue des objets : la forme de chacun et ses réglages de casse |
| [ChunkBuilder.cs](Assets/Scipts/ChunkBuilder.cs) | Construit un objet en petits cubes à partir d'une forme |
| [Breakable.cs](Assets/Scipts/Breakable.cs) | Casse un objet en cubes quand il prend un coup assez fort |
| [DesktopPlayer.cs](Assets/Scipts/DesktopPlayer.cs) | Joueur clavier/souris, utilisé quand il n'y a pas de casque |
| [Bat.prefab](Assets/Prefabs/Bat.prefab) | La batte (seul objet fait en prefab) |

### Qui appelle qui

```
Tête du joueur (caméra XR ou caméra clavier)
        │  position lue à chaque frame
        ▼
RoomDetector ── traverse le plan du détecteur ──► RoomManager.ChangeRoom(+1 ou -1)
                                                       │
                                  pièce déjà créée ?  ─┤ non ─► RoomGenerator.Generate(pièce, id)
                                                       │              │
                                                       │              ├─ PoissonDisk.Sample  (où mettre les meubles)
                                                       │              └─ RoomObjects.Table, Vase, ...
                                                       │                      └─ ChunkBuilder.Build (les cubes)
                                                       │                      └─ Breakable (+ XRGrabInteractable)
                                                       ▼
                                      affiche la nouvelle pièce, cache l'ancienne
```

### La scène

```
World            (RoomManager + RoomGenerator)
├── Ground       sol de 20 x 20 m
├── Walls        murs extérieurs + mur intérieur en L, tous de 3 m de haut
├── RoomDetector le détecteur (trigger vert)
├── RoomsParent  les pièces générées : Room_1000, Room_1001, ...
├── Bat          la batte, hors des pièces pour qu'elle reste avec le joueur
└── Directional Light
XR Origin (XR Rig)   le joueur en VR
DesktopPlayer        le joueur au clavier (un seul des deux est actif)
```

---

## 2. L'impossible space : RoomDetector et RoomManager

### Le principe

Un « impossible space » (Suma et al., 2012) fait tenir plusieurs pièces dans le même espace physique. Elles se superposent, et on change ce qu'il y a dedans au moment où le joueur ne peut pas le voir. Le joueur marche en rond dans une vraie salle, mais il a l'impression de traverser une suite de pièces différentes.

Pour que ça marche, il faut deux choses :
- Le changement doit se faire **hors du champ de vision** (Suma et al., 2011, change blindness). Les murs font donc 3 m de haut, et le détecteur est dans un couloir d'où on ne voit pas la pièce.
- Le joueur doit pouvoir **faire la boucle sans être bloqué**. Un passage le long du mur en L reste toujours libre.

### Le plan

Vu de dessus (nord = +z vers le haut, un caractère ≈ 0,5 m) :

```
 z=+4,75 ┌────────────────────────┐
         │                        │
         │  ┌──────────────┐      │
         │  │    zone des  │  ┃   │
         │  │    meubles   │  ┃   │  couloir
         │  └──────────────┘  ┃   │  est
         │   passage libre    ┃   │
         │    ━━━━━━━━━━━━━━━━┛   │
         │ couloir sud   ▲  [D]   │
 z=-4,75 └────────────────────────┘
       x=-5,75                  x=+5,75
         ▲ = départ   [D] = détecteur   ━ ┃ = mur intérieur en L
```

| Élément | Position |
|---|---|
| Murs extérieurs (faces intérieures) | x de -5,75 à 5,75 ; z de -4,75 à 4,75 |
| InnerWall1 (branche horizontale du L) | x de -3,75 à 3,75 ; z = -2,5 |
| InnerWall2 (branche verticale du L) | x = 3,5 ; z de -2,25 à 2,75 |
| Détecteur | x de 1 à 2 ; tout le couloir sud |
| Départ du joueur | (0,04 ; -3,63) |
| Zone des meubles (rectangle jaune) | x de -4,95 à 1,25 ; z de -0,25 à 3,95 |

La boucle passe ainsi : couloir sud vers l'ouest, ouverture ouest, grande pièce le long du L, ouverture nord-est, couloir est vers le sud, retour dans le couloir sud, puis le détecteur.

### RoomDetector : savoir de quel côté est le joueur

Le détecteur ne se sert pas de la physique (`OnTriggerEnter`). Il lit à chaque frame la position de la tête, donc de la caméra. C'est plus fiable : le rig XR a plusieurs colliders, et la caméra suit vraiment le joueur. Le champ `player` peut être rempli à la main. S'il est vide, on prend `Camera.main` à chaque frame, ce qui marche avec la caméra VR comme avec la caméra clavier.

1. **Passage en coordonnées locales.** `transform.InverseTransformPoint(head.position) - box.center` donne la position de la tête dans le repère du détecteur. Dans ce repère, la boîte est centrée en 0 et alignée sur les axes, donc les tests deviennent simples.
2. **Test « dans la boîte »**, sans tenir compte de la hauteur : |x| < largeur/2 et |z| < profondeur/2. Si le joueur sort de la boîte, on oublie le côté (`lastSide = 0`).
3. **Le côté** est le signe de z local, converti en mètres (`localZ * lossyScale.z`) :
   - +1 devant l'axe bleu du détecteur, -1 derrière ;
   - 0 dans une zone morte de 5 cm autour du plan, pour éviter les allers-retours quand on reste pile dessus.
4. **Changement de pièce** quand le côté passe de -1 à +1 (ou l'inverse) pendant qu'on est dans la boîte : `ChangeRoom(side)`. Le détecteur est tourné de 90°, donc son axe bleu pointe vers +x. Traverser vers l'est donne +1, vers l'ouest -1.

Comme on oublie le côté en sortant de la boîte, entrer dans le détecteur puis ressortir du même côté ne change rien.

### RoomManager : garder les pièces

- Les pièces sont dans un `Dictionary<int, GameObject>`. La première a l'id 1000, puis on fait +1 ou -1 à chaque passage.
- `ChangeRoom` crée la pièce si elle n'existe pas encore (`CreateRoom`), l'active, puis désactive la précédente.
- **Revenir en arrière** : faire -1 après +1 retombe sur la même clé du dictionnaire, donc sur le même GameObject. Les objets cassés le sont toujours, puisque la pièce a seulement été désactivée.
- Une pièce désactivée ne coûte rien : pas de rendu, pas de physique. Les débris programmés pour disparaître (`Destroy(cube, 5 s)`) disparaissent quand même, même si la pièce est cachée.
- `CurrentRoom` donne le Transform de la pièce affichée. Il sert à ranger un objet qu'on lâche.

### Porter un objet d'une pièce à l'autre

Le code source de XRI 3.3.2 (`XRGrabInteractable.Grab()` et `Drop()`) montre ce qui se passe :
- Quand on attrape un objet, XRI le sort de son parent (`SetParent(null)`). Il n'est donc plus dans la pièce, et il ne disparaît pas quand on traverse le détecteur.
- Quand on le lâche, XRI le remet dans son parent d'origine, mais seulement si ce parent est actif. Sinon il le laisse hors de toute pièce, et l'objet apparaîtrait alors dans toutes les pièces.

`RoomManager.CreateRoom` règle donc chaque objet attrapable avec `retainTransformParent = false`, et ajoute un écouteur sur `selectExited` : à la fin de la prise, l'objet est rangé dans la pièce courante. Si la prise est annulée (`args.isCanceled`, quand l'objet a éclaté dans la main), on ne fait rien.

---

## 3. La génération des pièces : RoomGenerator et PoissonDisk

### La graine : la même pièce à chaque fois

`Random.InitState(seed + id)` fixe le point de départ du générateur de nombres aléatoires. Avec la même graine, `Random.Range` renvoie toujours la même suite de nombres, donc la pièce 1003 est toujours la même. Pratique pour déboguer et pour présenter le projet. Changer `seed` dans l'inspecteur donne une autre série de pièces.

### Les couleurs

Une teinte au hasard parmi 12 : `hue = Random.Range(0, 12) / 12f`.
- Les meubles prennent cette teinte, peu saturée : `HSVToRGB(hue, 0.35, 0.7)`.
- Les petits objets prennent la teinte opposée sur le cercle des couleurs (`hue + 0.5`), plus saturée, pour ressortir sur les meubles.
- Les caisses sont toujours couleur bois.

Avec seulement 12 teintes, les matériaux sont réutilisés d'une pièce à l'autre (cache dans `ChunkBuilder.GetMaterial`).

### Placer les meubles : l'algorithme de Bridson

**Le problème.** On veut répartir des meubles dans la zone :
- sans qu'ils se chevauchent ;
- sans trous énormes ;
- sans que ça ressemble à une grille.

Un tirage au hasard simple fait des paquets et des chevauchements. Une grille est trop régulière. L'échantillonnage de Poisson (« Poisson disk sampling ») donne des points au hasard, mais **jamais plus proches qu'une distance r**. C'est le rendu « naturel » qu'on cherche.

**L'algorithme de Bridson (2007)** le fait en temps linéaire (`PoissonDisk.Sample`) :

1. **Grille d'accélération.** On découpe la zone en cellules de côté r/√2. La diagonale d'une cellule vaut alors (r/√2)·√2 = r. Deux points dans la même cellule seraient donc à moins de r l'un de l'autre, et chaque cellule contient au plus un point. La grille stocke le numéro du point (0 si la cellule est vide).
2. **Premier point** tiré au hasard dans la zone. Il est ajouté à la liste des points et à la liste « active », celle des points autour desquels il reste peut-être de la place.
3. **Tant qu'il reste des points actifs :**
   - on prend un point actif au hasard ;
   - on tire jusqu'à k = 30 candidats dans l'anneau entre r et 2r autour de lui (un angle au hasard, une distance au hasard entre r et 2r) ;
   - un candidat est accepté s'il est dans la zone et à au moins r de tous les points existants ;
   - si aucun des 30 n'est accepté, il n'y a plus de place autour de ce point, et il quitte la liste active.
4. **Vérifier un candidat est rapide.** Un point à moins de r ne peut pas être à plus de ⌈r / (r/√2)⌉ = ⌈√2⌉ = 2 cellules. Il suffit donc de regarder les 5 × 5 cellules autour du candidat, et non tous les points.

Pourquoi l'anneau [r, 2r] : en dessous de r, le candidat serait refusé à coup sûr ; au-delà de 2r, on laisserait des trous qu'un autre point aurait pu remplir.

**Les chiffres du projet :**
- `minDistance` = r = 1,8 m. Le plus grand meuble (une table de 1,32 × 0,84 m) a une demi-diagonale de √(0,66² + 0,42²) ≈ 0,78 m, et 2 × 0,78 = 1,56 < 1,8, donc deux meubles ne peuvent pas se toucher, même tournés.
- Dans une zone de 6,2 × 4,2 m, une simulation de 500 tirages donne 6,9 meubles en moyenne (entre 5 et 9).

### La zone des meubles

Le rectangle jaune (`zoneCenter`, `zoneSize`, visible dans la scène) a été choisi pour trois raisons :
- **À 0,8 m des murs** au moins, plus que la demi-diagonale d'un meuble : un meuble ne peut pas apparaître dans un mur. S'il le faisait, la physique l'éjecterait violemment et il casserait tout de suite.
- **À 1,2 m du mur en L** : c'est le passage libre pour faire la boucle.
- **Invisible depuis le détecteur** : avec des murs de 3 m, on ne voit pas cette zone depuis le couloir sud. Le changement de pièce passe inaperçu.

### Ce qu'on met à chaque endroit

Chaque point tiré reçoit un type de meuble, avec une rotation : un multiple de 90° (aligné sur les murs) ± 15° pour faire un peu désordre.

| Probabilité | Meuble | Ce qui va avec |
|---|---|---|
| 40 % | Table | 1 à 3 petits objets sur le plateau |
| 20 % | Étagère | 0 à 2 petits objets sur chacune des 4 planches |
| 25 % | Pile de caisses | 1 à 3 caisses, chacune un peu décalée (±5 cm) et tournée (±20°) |
| 15 % | Chaises | 1 ou 2 chaises côte à côte |

**Poser les petits objets (`AddItems`).** La surface est découpée en `count` emplacements égaux, et on met un objet au milieu de chacun : x = ((i + 0,5) / count − 0,5) × longueur. Sur une table, les emplacements font au moins 40 cm. Les objets font au plus 30 cm de large, donc ils ne se touchent jamais.

**Le centimètre de marge.** Chaque objet est posé 1 cm au-dessus de la surface, et chaque caisse 1 cm au-dessus de la précédente. Si deux objets apparaissaient l'un dans l'autre, le moteur physique les séparerait d'un coup, assez fort pour casser. Avec 1 cm, l'objet tombe à 0,44 m/s (v = √(2gh)), bien en dessous des seuils de casse.

### Taille d'une pièce

Une pièce fait entre 626 et 1341 cubes selon la graine (mesuré sur les ids 1000 à 1009). La générer prend 13 à 23 ms dans l'éditeur sur Mac, et l'afficher pour la première fois 5 à 14 ms.

---

## 4. Les objets en cubes : ChunkBuilder et RoomObjects

### L'idée : des voxels

Chaque objet est une grille 3D de cellules, comme des voxels. Une fonction dit pour chaque cellule (x, y, z) si elle est pleine : `Func<int, int, int, bool> isFull`. Chaque cellule pleine devient un petit cube Unity (`GameObject.CreatePrimitive(PrimitiveType.Cube)`, avec son BoxCollider).

Les coordonnées sont des **entiers**, ce qui rend les formes très simples à écrire. Par exemple, une table, c'est « la couche du haut, ou un coin » :

```csharp
(x, y, z) => y == height - 1 || (IsBorder(x, width) && IsBorder(z, depth))
```

### Placer un cube (`ChunkBuilder.Build`)

Pour une grille de nx × ny × nz cubes de côté c, le centre de la cellule (x, y, z) est :

```
position = ( (x − (nx − 1)/2) · c ,  (y + 0,5) · c ,  (z − (nz − 1)/2) · c )
```

- En x et z, on retire (n − 1)/2 pour que la grille soit **centrée** sur le pivot.
- En y, on ajoute 0,5 pour que la grille **commence au sol** (y = 0). Le pivot est donc en bas au centre, et on pose un objet au sol à y = 0, ou sur une table à y = hauteur de la table.
- Chaque cube a une taille de 0,98 · c. Il reste un **jour de 2 %** entre deux cubes, ce qui les rend visibles et donne le style « cubes ».

### Construit inactif

`ChunkBuilder.Build` renvoie l'objet **désactivé**. La raison tient à `Awake` : Unity n'appelle `Awake` qu'au moment où l'objet devient actif. Or `Breakable.Awake` a besoin de ses réglages (`Setup`) et de tous ses cubes. L'ordre est donc :
1. `Build` crée l'objet inactif et ses cubes ;
2. `RoomObjects` ajoute `Breakable` et appelle `Setup` ;
3. `RoomGenerator.Place` range l'objet dans la pièce et l'active.

La pièce elle-même est inactive pendant sa construction. `Awake` ne tourne donc que quand on entre dans la pièce.

### Le catalogue (`RoomObjects`)

| Objet | Forme | Taille du cube | Exemple de taille | Masse |
|---|---|---|---|---|
| Table | couche du haut + un pied à chaque coin | 12 cm | 10 × 6 × 6 → 80 cubes | 16 kg |
| Étagère | deux côtés + une planche toutes les 4 couches | 12 cm | 8 × 13 × 3 → 150 cubes | 30 kg |
| Chaise | assise + dossier (z = 0 au-dessus de l'assise) + 4 pieds | 12 cm | 4 × 8 × 4 → 44 cubes | 3,5 kg |
| Caisse | boîte creuse : seulement les cellules sur une face | 12 cm | 4 × 4 × 4 → 56 cubes | 4,5 kg |
| Vase | profil arrondi (voir plus bas) | 6 cm | 4 × 5 × 4 → 44 cubes | 0,5 kg |
| Bouteille | corps 3 × 3, goulot d'un cube sur les 2 dernières couches | 6 cm | 3 × 5 × 3 → 29 cubes | 0,6 kg |
| Assiette | disque d'un cube d'épaisseur | 6 cm | 5 × 1 × 5 → 21 cubes | 0,4 kg |

La masse de l'objet vaut toujours (nombre de cubes) × `chunkMass`. Quand il perd des cubes, il s'allège.

Les formes rondes utilisent `DistanceToAxis`, la distance entre la cellule et l'axe vertical au centre de la grille : √((x − c)² + (z − c)²) avec c = (largeur − 1)/2. Une cellule est pleine si cette distance est inférieure au rayon voulu.

**Le vase.** Son rayon suit une demi-sinusoïde le long de la hauteur h :

```
rayon(y) = col + (ventre − col) · sin(π · (y + 0,5) / h)
```

En bas et en haut, sin vaut presque 0 : on obtient le col (0,8 cube). Au milieu, sin vaut 1 : on obtient le ventre (la moitié de la largeur). Le vase est étroit aux extrémités et large au milieu.

### Deux familles d'objets

| | Meubles (table, étagère) | Objets attrapables (chaise, caisse, vase, bouteille, assiette) |
|---|---|---|
| Se prend en main | non | oui (`XRGrabInteractable`) |
| Casse | autour de l'impact (rayon 25 cm), le reste tient | **éclate en entier** (voir 5.5) |
| Vitesse minimale | 3,5 m/s | 4 m/s (chaise, caisse) ; 2,5 m/s (vase, bouteille, assiette) |
| Impulsion minimale | 3 N·s | 1 N·s (chaise, caisse) ; 0,3 N·s (vase, bouteille, assiette) |

---

## 5. La casse : Breakable

### 5.1 Structure d'un objet cassable

```
Table            Rigidbody (un seul pour tout l'objet), Breakable, MeshFilter + MeshRenderer (mesh combiné)
├── Chunk_0_0_0  BoxCollider (+ MeshRenderer désactivé)
├── Chunk_0_1_0  BoxCollider
└── ...
```

Un seul Rigidbody avec plusieurs colliders enfants forme un **collider composé** : Unity traite l'ensemble comme un seul corps rigide. Les cubes sont les enfants qui ont un collider. On filtre comme ça parce que XRI ajoute aussi un enfant « attach » quand on attrape un objet.

Unity recalcule tout seul le centre de masse et l'inertie quand on retire des colliders. On met juste à jour la masse.

### 5.2 Quand est-ce que ça casse ?

À chaque `OnCollisionEnter`, on calcule deux choses.

**La vitesse du choc**, le long de la normale du contact :

```
v = | relativeVelocity · normale |
```

On ne garde que la composante qui rapproche les deux objets. Un objet qui glisse le long d'un mur ne casse pas.

**L'impulsion du choc.** D'après le modèle classique de réponse aux collisions (impulse-based), l'impulsion échangée vaut :

```
J = (1 + e) · v / (1/m1 + 1/m2)
```

On prend e = 0 (choc mou), et on reconnaît la **masse réduite** μ = 1 / (1/m1 + 1/m2) = m1·m2 / (m1 + m2) :

```
J = μ · v
```

Si l'autre objet est un mur ou un objet kinematic, sa masse est considérée infinie : 1/m2 = 0, donc μ = m1.

**L'objet casse si v ≥ minSpeed ET J ≥ minImpulse.** Il faut les deux conditions, et chacune bloque un cas précis :
- Un débris va vite, mais il est léger : petite impulsion, donc la condition sur J le bloque.
- Une table qui tombe est lourde, mais va lentement : petite vitesse, donc la condition sur v la bloque.

| Situation | v | μ | J | Résultat (table : 3,5 m/s et 3 N·s) |
|---|---|---|---|---|
| Batte (1 kg) sur une table (16 kg) | 5 m/s | 0,94 kg | 4,7 | **casse** |
| Débris de 0,2 kg sur une table | 5 m/s | 0,2 kg | 1,0 | rien |
| Table poussée doucement | 1 m/s | | | rien |

| Situation | v | J | Résultat (vase : 2,5 m/s et 0,3 N·s) |
|---|---|---|---|
| Vase (0,53 kg) lancé à 8 m/s contre un mur | 8 m/s | 4,2 | **éclate** |
| Vase tombé de 50 cm (v = √(2gh) = 3,1 m/s) | 3,1 m/s | 1,7 | **éclate** |
| Vase posé au sol | 0 | 0 | rien |

**Pourquoi on ne lit pas `collision.impulse`.** C'est ce que faisait la première version, et les tests ont montré deux problèmes :
1. Avec un collider composé, Unity envoie un `OnCollisionEnter` **par cube en contact** et partage l'impulsion entre eux. Chaque cube ne reçoit qu'une petite part.
2. Avec la détection de collision continue (`ContinuousSpeculative`, réglée sur les objets attrapables), le moteur arrête l'objet **juste avant** le contact. L'impulsion rapportée à l'entrée vaut alors 0 : un vase lancé à 8 m/s contre un mur donnait 0 pour chaque cube, et ne cassait pas.

`relativeVelocity`, lui, est fiable. On recalcule donc l'impulsion à partir des vitesses et des masses.

`IsHeld` (voir la partie 7) bloque aussi la casse pendant qu'on tient l'objet au clavier.

### 5.3 Étape 1 : détacher les cubes près de l'impact

Tous les cubes dont le centre est à moins de `breakRadius` (25 cm) du point de contact se détachent (`Detach`) :
- Le cube quitte l'objet et va dans la pièce (`SetParent(transform.parent)`), pour qu'il soit caché avec elle.
- Son MeshRenderer est réactivé, et il reçoit son propre Rigidbody.
- Sa vitesse de départ est celle qu'il avait en tant que morceau de l'objet, plus une poussée qui l'écarte de l'impact :

```
v_cube = v_objet + ω × (p_cube − centre_de_masse)   ← Rigidbody.GetPointVelocity
       + 2 m/s · direction(impact → cube)
```

Le premier terme compte la rotation de l'objet : un cube au bout d'une chaise qui tourne part plus vite qu'un cube au centre.

- Le cube est détruit au bout de 5 s (`chunkLifetime`), pour ne pas accumuler des milliers d'objets.

On parcourt la liste **à l'envers**, parce qu'on enlève des éléments pendant la boucle.

### 5.4 Étape 2 : trouver les morceaux qui ne tiennent plus (BFS)

Après un coup, l'objet peut être coupé en plusieurs morceaux. Par exemple, un pied de table dont le haut a sauté ne touche plus le plateau. Il doit tomber.

**Le test de contact (`AreTouching`).** Deux cubes se touchent si leurs boîtes se chevauchent. Les cubes ne sont pas tournés à l'intérieur de l'objet, donc ce sont des boîtes alignées sur les axes (AABB). Le test (Ericson, *Real-Time Collision Detection*, §4.2) dit que deux AABB se chevauchent si, sur chacun des trois axes, la distance entre leurs centres est inférieure à la somme de leurs demi-tailles :

```
|Δx| < (sa + sb)/2 · 1,1   et   |Δy| < ...   et   |Δz| < ...
```

La marge de 10 % sert à combler le jour de 2 % entre les cubes. Deux cubes voisins sont à une distance c (le pas de la grille), et la distance de contact vaut (0,98c + 0,98c)/2 × 1,1 = 1,078c > c : ils se touchent. Deux cubes séparés d'une case sont à 2c > 1,078c : ils ne se touchent pas. Les cubes qui ne se touchent que par une arête ou un coin comptent aussi (26 voisins par cube).

**Le parcours en largeur (`FindParts`).** C'est un flood fill, comme le pot de peinture d'un logiciel de dessin :
1. On met tous les cubes restants dans une liste `left`.
2. On prend le premier cube : il commence un nouveau morceau `part`.
3. On parcourt `part` avec un indice i. Pour chaque cube `part[i]`, tous les cubes de `left` qui le touchent passent dans `part`. Comme on les ajoute à la fin de la liste qu'on est en train de parcourir, **la liste `part` sert elle-même de file d'attente** au parcours en largeur.
4. Quand i arrive au bout de `part`, le morceau est complet. S'il reste des cubes dans `left`, on recommence à l'étape 2 avec un nouveau morceau.

Le coût est O(n²) dans le pire cas (chaque cube est comparé aux cubes restants). Avec 150 cubes au plus par objet, ça fait au plus environ 20 000 tests par casse, négligeable.

### 5.5 Étape 3 : le plus gros morceau reste, les autres tombent

- Les morceaux sont triés par taille. Le plus gros reste l'objet d'origine.
- Un morceau d'au moins 4 cubes devient un **nouvel objet cassable** (`CreatePiece`) :
  - nouveau GameObject inactif ;
  - on y déplace les cubes ;
  - `Breakable` + `Setup` avec les mêmes réglages ;
  - on l'active ;
  - il reprend la vitesse et la rotation de l'objet d'origine.
- Un morceau plus petit part en débris.
- Si plus aucun cube ne reste, l'objet est détruit.

**Pourquoi les objets attrapables éclatent en entier.** Le code source de XRI 3.3.2 (`XRInteractionManager.RegisterInteractable`) le montre : l'Interaction Manager garde une table « collider → objet attrapable », remplie **une seule fois**, quand l'objet s'enregistre. Si un cube tombait d'un vase et que le reste du vase restait entier, le collider du cube serait toujours lié au vase : en attrapant le débris, on attraperait le reste du vase, à distance. Quand l'objet éclate en entier, il est détruit, et XRI ignore les liens vers un objet détruit. C'est aussi plus logique pour de la céramique ou du verre.

### 5.6 Le mesh combiné : un seul dessin par objet

Dessiner 1000 cubes, c'est 1000 draw calls (commandes de dessin envoyées au GPU), trop pour un Quest. Donc :
- Les MeshRenderer des cubes sont désactivés.
- L'objet dessine **un seul mesh** qui contient tous ses cubes (`Mesh.CombineMeshes`). Chaque cube y est placé avec la matrice `TRS(position locale, rotation locale, échelle locale)`.
- Après chaque casse, le mesh est recalculé avec les cubes restants (`UpdateObject`).
- Les débris réactivent leur propre MeshRenderer.

Une pièce intacte coûte ainsi environ 20 draw calls (un par objet) au lieu d'environ 1000. Un mesh créé par code n'est pas libéré avec l'objet, donc on le détruit nous-mêmes dans `OnDestroy`.

---

## 6. L'interaction VR : XR Interaction Toolkit et la batte

### Les réglages des objets attrapables (`RoomObjects.MakeGrabbable`)

| Réglage | Pourquoi |
|---|---|
| `movementType = VelocityTracking` | Par défaut (Instantaneous), XRI rend l'objet tenu **kinematic**. D'après la matrice des collisions de Unity, un objet kinematic contre un collider statique (un mur) ne déclenche pas `OnCollisionEnter`, et il passe à travers. En Velocity Tracking, XRI donne à l'objet la vitesse qui l'amène à la main : il reste un vrai corps physique, il heurte les murs et il casse. |
| `useDynamicAttach = true` | On attrape l'objet là où la main le touche, au lieu que son pivot (en bas au centre) saute dans la main. |
| `interpolation = Interpolate` | La physique tourne à 50 Hz et l'affichage du casque à 72 Hz ou plus. L'interpolation lisse le mouvement entre deux pas de physique. |
| `collisionDetectionMode = ContinuousSpeculative` | Un objet lancé vite ne traverse pas un objet fin entre deux pas de physique. |

### La batte ([Bat.prefab](Assets/Prefabs/Bat.prefab))

- **Forme :** quatre primitives (pommeau, manche, fût, bout arrondi), 85 cm de long, 1 kg.
- **Pas de `Breakable`** : la batte ne casse jamais.
- **Physique :** `VelocityTracking` et `ContinuousDynamic`, pour qu'un coup rapide ne traverse pas une table.
- **Point d'attache :** l'enfant `Attach`, sur le manche, tourné de -45° autour de x. XRI fait coïncider ce point avec la main. La batte part donc vers l'avant et vers le haut, à 45° de la direction de la manette.
- **Placement :** elle est dans `World`, pas dans une pièce. Elle reste avec le joueur quand il change de pièce. Quand on la lâche, XRI la remet dans `World` (son parent d'origine, toujours actif).

### Le simulateur XRI

L'apparition automatique du XR Interaction Simulator est désactivée (`XRDeviceSimulatorSettings`) : le mode clavier le remplace. Pour le remettre : Project Settings → XR Plug-in Management → XR Interaction Toolkit.

---

## 7. Le mode clavier : DesktopPlayer

### Choisir le mode au démarrage

Dans `Awake`, `XRSettings.isDeviceActive` dit si un casque est actif. XR Management initialise le casque avant le chargement de la scène, donc la réponse est déjà connue.
- **Casque actif** (et `forceDesktop` décoché) : `DesktopPlayer` se désactive, et on joue avec le rig XR.
- **Pas de casque** (toujours le cas sur Mac, où OpenXR n'existe pas) : le rig XR est désactivé, et la caméra du `DesktopPlayer` devient `Camera.main`. Le détecteur la suit automatiquement.

### Les commandes

| Touche | Action |
|---|---|
| ZQSD ou flèches | Se déplacer (2,5 m/s) |
| Souris | Regarder |
| Clic gauche | Prendre l'objet sous la croix ; avec la batte : frapper ; avec un objet : le lancer à 8 m/s |
| Clic droit | Lâcher |
| Échap | Libérer la souris (un clic la reprend) |

Les touches sont lues par leur **place** sur le clavier (Input System) : `wKey` est la touche en haut à gauche, W en QWERTY et Z en AZERTY.

### Se déplacer et regarder

- **La souris :** son déplacement horizontal tourne le **corps** (`transform.Rotate`), son déplacement vertical incline seulement la **caméra** (`pitch`, limité à ±85° pour ne pas passer la tête à l'envers). Le corps reste vertical, et ZQSD avance dans la direction où on regarde horizontalement.
- **Le déplacement :** `CharacterController.SimpleMove` gère la gravité et les collisions avec les murs. Le joueur ne traverse ni les murs ni les meubles, ce qui garde l'illusion de l'impossible space.

### Prendre un objet (`TryGrab`)

1. **Viser.** Un `SphereCast` de 5 cm de rayon part de la caméra, jusqu'à 3 m. Pourquoi pas un simple rayon : un rayon infiniment fin peut passer dans le jour de 2 % entre deux cubes et toucher le sol derrière. Ça arrivait en test quand on visait pile le centre d'un vase.
2. **Garder seulement ce qui s'attrape en VR** : il faut un `XRGrabInteractable` sur l'objet touché ou sur un de ses parents.
3. **Préparer l'objet :**
   - on le sort de sa pièce, pour pouvoir le porter à travers le détecteur ;
   - on coupe la gravité ;
   - on monte `maxAngularVelocity` à 50 rad/s (la limite par défaut, 7 rad/s, est trop lente pour un coup de batte) ;
   - on ignore les collisions avec le corps du joueur (`Physics.IgnoreCollision`), sinon l'objet tenu pousserait la capsule du joueur et casserait ;
   - on met `IsHeld = true` (voir plus bas).
4. **Retenir le point de prise**, comme un attach transform, dans le repère de l'objet :
   - pour la batte, son point `Attach` ;
   - pour un autre objet, le point visé, avec l'orientation de la caméra à ce moment-là. L'objet garde ensuite la même orientation par rapport à la caméra.

### Faire suivre la main (`FixedUpdate`)

**Où est la main :**
- pour un objet : sur l'axe de la caméra, à 1 m (moins si un mur est devant) ;
- pour la batte : un point fixe à droite de la caméra (`handPosition`), tourné par le coup (voir plus bas).

**Où doit être l'objet.** On veut que le point de prise (position locale p_g, rotation locale R_g) arrive pile dans la main (position p_m, rotation R_m). En monde, le point de prise est en p_objet + R_objet · p_g, avec la rotation R_objet · R_g. On l'égale à la main :

```
R_objet · R_g = R_m           →   R_cible = R_m · R_g⁻¹
p_objet + R_objet · p_g = p_m →   p_cible = p_m − R_cible · p_g
```

C'est exactement ce que fait XRI avec un attach transform.

**Velocity tracking.** On ne téléporte pas l'objet : on lui donne la vitesse qui l'amène à la cible en un pas de physique (Δt = 0,02 s). Comme ça, il reste un corps physique qui heurte les choses.

```
vitesse          = (p_cible − p_objet) / Δt        limitée à 15 m/s
rotation restante = R_cible · R_objet⁻¹  →  (angle θ, axe u)
vitesse angulaire = u · θ / Δt          (θ en radians, ramené entre -180° et 180°)
```

La limite de 15 m/s évite qu'un objet bloqué derrière un obstacle parte à une vitesse folle quand il se libère. On ramène l'angle entre -180° et 180° pour tourner par le chemin le plus court.

### Le coup de batte

Le clic lance un chrono `swingTime`. La rotation de la main passe de la pose de repos (`restAngles`, la batte levée à droite) à la pose de frappe (`strikeAngles`, la batte en bas à gauche) :

```
progression s : 0 → 1 en 0,15 s (frappe), puis 1 → 0 en 0,3 s (retour)
rotation main = Slerp(repos, frappe, s)
```

`Slerp` interpole entre deux rotations à vitesse angulaire constante. Les deux poses sont écartées d'environ 117°, parcourus en 0,15 s, soit environ 13,7 rad/s. La pointe est à environ 85 cm de la main, ce qui donne environ 11,6 m/s en théorie au bout de la batte. On en mesure 9,8, parce que la batte suit la main avec un léger retard. C'est largement au-dessus des seuils de casse. La batte suit cette main par velocity tracking, comme un objet : elle tape vraiment, elle s'arrête sur les murs, et la casse passe par `Breakable` comme pour un coup donné en VR.

### Les murs et la casse dans la main

- **Distance de tenue (`GetHoldDistance`).** Si un mur ou le sol (un collider sans Rigidbody) est à moins de 1,3 m devant, l'objet est tenu 30 cm avant ce mur, mais jamais à moins de 40 cm de la caméra. Sinon, la main serait derrière le mur, et le suivi enfoncerait l'objet dedans.
- **`IsHeld`.** Avec une souris, on ne contrôle pas finement la « main ». Un quart de tour rapide fait fouetter l'objet tenu à 15 m/s contre un mur, et le test l'a cassé comme ça. Au clavier, un objet tenu ne casse donc pas lui-même : **on le lance pour le casser**. Il casse toujours ce qu'il heurte, puisque c'est l'autre objet qui calcule son propre choc. En VR, rien ne change : on contrôle sa main, et écraser un vase qu'on tient contre un mur le casse.

### Lâcher ou lancer (`Release`)

- On remet la gravité, les collisions avec le joueur, et `IsHeld = false`.
- **Lancer :** vitesse = direction de la caméra × 8 m/s. **Lâcher :** l'objet garde la vitesse qu'il avait en suivant la main.
- **Le parent**, même règle qu'en VR : si la pièce d'origine est encore affichée (ou `World` pour la batte), l'objet y retourne. Sinon, il va dans la pièce où se trouve le joueur.

---

## 8. Réglages

| Réglage | Où | Effet |
|---|---|---|
| `seed` | RoomGenerator (inspecteur) | Une autre série de pièces |
| `zoneCenter`, `zoneSize` | RoomGenerator | La zone des meubles (rectangle jaune) |
| `minDistance` | RoomGenerator | Distance mini entre meubles : plus grand = moins de meubles |
| `FurnitureCube`, `ItemCube` | RoomObjects.cs | Taille des cubes : plus gros = moins de cubes, plus rapide sur Quest |
| seuils de casse | `MakeFurniture`, `MakeGrabbable`, et les appels dans RoomObjects.cs | Plus ou moins facile à casser |
| `breakRadius`, `explosionSpeed`, `chunkLifetime` | Breakable | Taille du trou, vitesse et durée de vie des débris |
| `Force Desktop` | DesktopPlayer | Mode clavier même avec un casque |
| `lookSpeed`, `moveSpeed` | DesktopPlayer | Sensibilité de la souris, vitesse de marche |
| `handPosition`, `restAngles`, `strikeAngles` | DesktopPlayer | Position de la batte et geste du coup |
| `holdDistance`, `throwSpeed` | DesktopPlayer | Distance de l'objet tenu, force du lancer |

---

## 9. Tests réalisés

Tous lancés dans Unity en Play mode (batchmode). Le batchmode ne simule pas une vraie souris : les tests du mode clavier visent par code et appellent directement les fonctions de prise, de coup et de lâcher.

| Test | Résultat |
|---|---|
| 10 pièces générées (ids 1000 à 1009), 3 s de physique | entre 626 et 1341 cubes, aucune casse au chargement |
| Boule de 1 kg sur une table à 1 m/s | rien |
| Boule de 1 kg sur une table à 7 m/s | des cubes partent (6 sur 80) |
| Vase posé au sol / lâché de 50 cm / lâché de 1,2 m | intact / éclate / éclate |
| Chaise lâchée de 50 cm sur une caisse | les deux restent intactes |
| Coup à la jonction d'un pied (table haute) | le pied part comme objet séparé (6 cubes, 1,2 kg) |
| MainScene : pièce 1000 → 1001 → retour | la pièce 1000 est retrouvée avec ses 19 objets |
| Mode clavier au démarrage sans casque | rig XR désactivé, caméra clavier active, pas de simulateur |
| Batte prise par le manche, 3 coups sur une table | table de 80 à 70 cubes, pointe à 9,8 m/s ; lâchée, la batte retourne dans `World` |
| Vase porté à travers le détecteur, puis lâché | pièce 1001 affichée, vase intact, rangé dans `Room_1001` |
| Vase tenu face à un mur à 0,88 m | intact (tenu 30 cm avant le mur) |
| Vase lancé à 8 m/s contre le mur | éclate |

---

## 10. Limites connues

- **Pas encore testé au casque** ni dans un build Quest.
- **Petit à-coup au détecteur :** créer une pièce prend 13 à 23 ms sur Mac, et ce sera plusieurs fois plus long sur Quest. Ça tombe dans le couloir, sans voir la pièce. Amélioration possible : préparer les pièces voisines à l'avance, sur plusieurs frames.
- **Fixed Timestep à 0,02 s** (50 Hz) : le passer à 1/72 s suivrait mieux la fréquence du casque pour la batte, mais demande 44 % de calculs physiques en plus.
- **Réactions en chaîne :** un débris de meuble (0,2 kg) peut casser un vase. C'est voulu, mais une pièce très chargée peut s'écrouler en cascade.
- **Seuils serrés pour les meubles :** un plateau qui perd ses pieds tombe de 60 cm (la face du dessous) et arrive à √(2 × 9,81 × 0,6) ≈ 3,4 m/s, juste sous le seuil des meubles (3,5 m/s). Il ne se recasse pas, mais de peu : baisser ce seuil ferait s'effondrer les tables en cascade.
- **Contact par une arête :** les cubes qui ne se touchent que par une arête comptent comme reliés, ce qui rend les morceaux un peu plus solides que dans la réalité.
