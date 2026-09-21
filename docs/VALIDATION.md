# Validation de la livraison

**Date : 21 septembre 2026. Version des sources : 0.1.0.**

## Résultats réellement obtenus

| Vérification | Résultat | Portée exacte |
|---|---|---|
| Compilation TypeScript | Réussie | `npm test` exécute `tsc -p tsconfig.json` |
| Vérification des types TypeScript | Réussie | `npm run typecheck`, mode strict |
| Tests TypeScript | **26/26 réussis**, aucun ignoré | Validation du transport et de réponses sur fixtures |
| Transport HTTP natif | Réussi | Un des 26 tests utilise un serveur HTTP Node local |
| Structure de la solution | Réussie | Références locales, absence de cycles, présence des projets |
| Formats JSON et XML | Réussis | Parsing des configurations, projets et propriétés |
| Noms des champs publics .NET / TS | Alignés | Vérification statique des DTO ; pas d'exécution JSON .NET |
| Script Bash | Syntaxe valide | `bash -n`, sans exécution des commandes .NET |
| Préparation du package npm | Dry-run réussi | Construction du package local, aucune publication |

Environnement réellement utilisé : Debian 13 x64, Node.js **22.16.0**, npm **10.9.2**,
TypeScript **5.8.3**. Le build TypeScript a utilisé le compilateur déjà installé ;
`npm install` et la création d'un lockfile npm n'ont pas été exécutés.

## Vérifications non exécutées

| Vérification | État | Motif ou limite |
|---|---|---|
| NuGet restore | Non exécuté | SDK .NET absent de l'environnement |
| Compilation C# | Non exécutée | Aucun `dotnet` disponible ; récupération du SDK impossible |
| Tests .NET | Non exécutés | Même limite ; aucun test C# déclaré réussi |
| Hôte ASP.NET Core réel | Non lancé | Aucun runtime/SDK .NET disponible |
| Client TypeScript vers API .NET | Non exécuté | Hôte .NET non lancé ; script de smoke test fourni |
| Build Next.js | Non exécuté | Exemple d'intégration, sans application Next.js complète |
| Vérification PowerShell | Non exécutée | Script fourni ; pas de validation par un interpréteur PowerShell local |
| PostgreSQL / migration / multi-bases | Non implémenté, donc non testé | Étape de raccordement suivante |
| RBAC / rotation / auth / MFA | Non implémenté, donc non testé | Audit de l'existant et intégration encore nécessaires |

Le fichier de tests C# contient **34 méthodes Fact**, **4 méthodes Theory** et
**21 jeux InlineData**, soit **55 cas déclarés par inventaire statique**.
Cela ne représente pas un résultat de découverte ou d'exécution xUnit.
Les tests API utilisent `WebApplicationFactory` ; même après succès, ils ne remplacent
pas les futurs tests contre PostgreSQL, le RBAC et les pannes réelles.

## Reproduire sous Windows

Depuis le répertoire contenant `IdentityAccess.sln`, avec un SDK .NET 10 à jour :

```powershell
dotnet restore IdentityAccess.sln
dotnet build IdentityAccess.sln -c Release --no-restore
dotnet test IdentityAccess.sln -c Release --no-build --no-restore
```

Le script `scripts/verify.ps1` regroupe ces commandes, vérifie les codes de sortie et
écrit le TRX dans `artifacts/test-results/identity-access.trx`.
Les avertissements C# sont traités comme des erreurs. Les dépendances de test sont
versionnées explicitement ; le graphe NuGet complet reste à restaurer et valider.

Ensuite, lancer l'API dans un autre terminal :

```powershell
dotnet run --project src/IdentityAccess.Api --launch-profile http
```

Depuis `clients/typescript`, après installation des dépendances de développement :

```powershell
npm test
npm run smoke -- http://127.0.0.1:5080
```

Le smoke test vérifie les diagnostics contre cet hôte. La readiness `503` / `ready: false`
est l'état attendu de cet incrément, pas un échec à contourner ni une validation de sécurité.

## Éléments de preuve inclus

`docs/validation/typescript-tests.txt` contient la sortie des 26 tests exécutés.
`docs/validation/typescript-typecheck.txt` contient la vérification des types.
`docs/validation/source-checks.json` détaille les vérifications structurelles et
la distinction entre tests passés et vérifications non exécutées.

**Statut du pack : sources disponibles ; gate .NET en attente d'exécution.
Aucun statut de production prête, de compatibilité RBAC acquise ou de persistance
multi-bases opérationnelle n'est annoncé.**
