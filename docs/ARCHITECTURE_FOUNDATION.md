# Fondations du sous-projet Identity & Access

**Date : 21 septembre 2026. Version des sources : 0.1.0.**

## Décisions reprises

Le serveur est une API logique commune ASP.NET Core. Le stockage cible est PostgreSQL,
avec plusieurs bases possibles et un placement résolu côté serveur. Une application
peut utiliser plusieurs destinations ; une destination peut héberger plusieurs tenants
lorsque les contraintes d'isolation le permettent. Ni une base globale unique ni une
base imposée par application ne sont retenues.

MAGELLAN et Console Runtime embarquent chacune leur module UI et leur client. Le contexte
applicatif vient de l'intégration ; il ne constitue pas une permission. Aucun écran ne
demande de choisir entre ces deux projets. Choisir un tenant autorisé est un autre besoin.

Le nom MAGELLAN remplace l'ancienne appellation AXENTRA dans les nouveaux éléments.
Les fichiers d'architecture source fournis ne sont pas modifiés par cette livraison.

## Code effectivement créé

| Projet | Responsabilité actuelle |
|---|---|
| `IdentityAccess.Domain` | Références d'identité immuables, états User/Tenant/Group, appartenances et contrôles structurels |
| `IdentityAccess.Contracts` | DTO publics de profils et diagnostics, sans secret ni destination physique |
| `IdentityAccess.Application` | Projection explicite des profils et description du statut de fondation |
| `IdentityAccess.Api` | Composition de l'hôte, diagnostics locaux, refus de démarrage en Production |
| `IdentityAccess.Tests` | Tests de modèles, projections, contrats et hôte ; non exécutés ici |

La dépendance va de l'API vers Application/Contracts et d'Application vers Domain/Contracts.
Le domaine et les contrats n'ont aucun package externe. Aucune référence au moteur runtime,
à Redis, à Npgsql ou à EF Core n'est introduite dans ce premier incrément.

Le client TypeScript appelle les diagnostics HTTP. L'exemple Next.js importe `server-only`.
Il n'expose ni sessions, ni tokens, ni évaluateur RBAC, ni rotation de contexte.

## Périmètre logique d'identité

`IdentityScopeId` matérialise le périmètre d'identité déjà requis par la roadmap.
La référence d'un utilisateur est `(IdentityScopeId, UserId)` et celle d'un tenant
`(IdentityScopeId, TenantId)`. Un groupe ajoute l'application et son `GroupId`.

Cela permet de représenter deux comptes distincts portant le même identifiant local
sans les fusionner. Une référence reste stable quand le placement physique change.
L'e-mail n'est pas utilisé comme identifiant technique.

**Cette forme de référence est une proposition de contrat de fondation, pas la
sélection silencieuse d'un annuaire global ni d'une politique de SSO.** La façon
opérationnelle d'attribuer les scopes, de partager un annuaire et de produire le
subject OIDC reste à figer. Aucun token ni table SQL ne matérialise encore ces choix.

Les clés d'application du code sont sensibles à la casse, sans normalisation implicite :
1 à 64 caractères, première lettre minuscule, puis lettres minuscules, chiffres ou tirets.
Cette contrainte est explicite et testable ; ce n'est pas la grammaire TRN.

## Appartenances et groupes

Un utilisateur peut appartenir à plusieurs tenants dans un même périmètre d'identité.
Le constructeur d'une appartenance rejette les scopes divergents. L'ajout structurel
à un groupe rejette un tenant différent, y compris lorsque deux scopes réutilisent
le même `TenantId` local. Il exige un groupe et une appartenance au tenant actifs.

Le statut du compte est séparé du statut de son appartenance à un tenant. Les objets
sont immuables ; aucun endpoint ne permet de modifier ces états.

L'appartenance de groupe porte son contexte applicatif. Elle ne crée pas de droits
sur une autre application. Un `UserGroup` n'est jamais le `TenantGroupId` du moteur.
Les permissions, leurs bindings et les ressources exactes ne sont pas encore implémentés.

Les contrôles de construction sont nécessaires mais insuffisants pour l'exploitation :
il faut encore autoriser l'administrateur, vérifier l'état actuel du compte et du tenant,
appliquer les contraintes SQL, gérer les mutations concurrentes, auditer et révoquer.
Une instance de domaine construite par un appelant n'est jamais un contexte de confiance.

## Profils publics

Les projections n'exposent que les références, le nom affiché et le statut approprié.
Elles n'exposent pas de credential, facteur MFA, refresh token ou information de connexion.
Le mapper ne constitue pas une autorisation : le futur service de lecture devra autoriser
chaque scope et chaque sujet avant de retourner une projection, y compris par lots.

Aucun e-mail de connexion, secret ou mécanisme de récupération n'est ajouté avant d'avoir
figé l'unicité et le stockage des comptes. Ce sont des fonctions manquantes explicites,
pas un stockage de démonstration présenté comme une authentification.

## PostgreSQL et routage : prochaine frontière

Aucun schéma n'est figé dans cet incrément. Le prochain contrat devra représenter la
route demandée par l'opération serveur, une destination enregistrée, une révision et
son état administratif. Le fournisseur initial sera un fichier, le catalogue SQL étant
optionnel derrière le même contrat. Les références de secrets resteront côté serveur.

Une route absente, ambiguë ou désactivée doit bloquer l'opération. Aucun fallback vers
une base par défaut. Une opération devra conserver sa destination et sa version de route.
La co-localisation nécessaire aux opérations atomiques sera définie avant de séparer
les familles de tables. Les clés étrangères et transactions locales ne seront pas
présentées comme des garanties inter-bases.

Les choix bloquants restent visibles : partage ou séparation des annuaires, placement
des utilisateurs multi-tenants, localisation de l'annuaire avant authentification,
relation entre compte et subject OIDC, et unité routée initiale. Ce dépôt ne choisit
pas une réponse en cachant une base globale dans sa configuration.

## Sécurité et compatibilité existante

Il n'y a pas de mock `Allow`, d'administrateur prédéfini, de mot de passe partagé,
de création de compte anonyme ou de mécanisme cryptographique maison.
`RequireCapability`, `IAuthorizationEngine` et `X-Access-Context` ne sont pas réimplémentés
à partir des seuls noms cités dans la roadmap.

Le futur adaptateur préservera les garanties effectivement trouvées dans le code .NET :
réhydratation, isolation des traitements concurrents, rotation activée ou désactivée,
expiration, révocation et protection des traitements en cours. La compatibilité n'est
pas acquise par le simple ajout d'un endpoint Allow/Deny.

## Limite de livraison

Cette livraison amorce le pack de modèles et frontières ; elle ne ferme pas le gate de
compatibilité RBAC, ne fournit pas une base migrée et ne constitue pas un service d'identité
prêt pour la production. L'exécution du build et des tests .NET demeure à obtenir.

## Référence de travail

`IDENTITY_ACCESS_MULTIDATABASE_ARCHITECTURE_ROADMAP_v1 (2).md`, version 1.0 du
21 septembre 2026, sections 1, 4, 5, 9, 11, 12, 18, 20 et 21.

SHA-256 de la copie source fournie :
`39d8f298ca1f4a39318004018b0c23606556f7aa6d08e00e7795b3db23f05420`.

Les propositions et code de ce dépôt ne modifient pas rétroactivement le statut de
la roadmap, qui restait un cadrage avant implémentation.
