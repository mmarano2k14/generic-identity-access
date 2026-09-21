# Intégration Next.js côté serveur

Cet exemple est destiné à être intégré dans MAGELLAN et dans Console Runtime. Il ne
crée pas une nouvelle application Next.js et n'ajoute pas de sélecteur de projet.
Le serveur Next.js appelle l'API .NET ; ce connecteur n'est pas un accès PostgreSQL.

Depuis `identity-access/clients/typescript`, construire l'archive locale :

```powershell
npm install
npm test
npm pack
```

Depuis l'application Next.js consommatrice, installer **le chemin réel** de l'archive
`identity-access-client-0.1.0.tgz` et `server-only` :

```powershell
npm install "C:\chemin\identity-access\clients\typescript\identity-access-client-0.1.0.tgz" server-only
```

Le chemin est un exemple à remplacer. Le package n'est pas publié sur npm.
Copier `identity-access.ts` dans le dossier d'intégration serveur de l'application.
Configurer `IDENTITY_ACCESS_API_BASE_URL` côté serveur avec l'exemple `.env.example`.
Ne pas utiliser le préfixe `NEXT_PUBLIC_` pour cette configuration.

```typescript
// Dans un module serveur de l'application, avec le chemin local approprié.
const info = await identityAccessDiagnostics().info();
```

L'import `server-only` doit empêcher l'import accidentel de ce module depuis un composant
client dans l'application Next.js. Ce comportement n'a pas été testé ici avec un build
Next.js : aucune application Next.js ni dépendance Next.js n'est incluse.

L'exemple n'établit aucune session et ne protège aucune route métier. Il ne doit pas être
utilisé comme preuve d'autorisation. Le futur adaptateur de sécurité devra préserver les
contrats du serveur et du moteur RBAC audité ; il ne réécrira pas la rotation en TypeScript.
