# Hashtag Wall

Sistema web para **monitorar uma hashtag do Instagram** (via **Instagram Graph API** / **Meta**) e exibir fotos em um **mural em tela cheia**, com foco configurável por slide (padrão **30 segundos**), **moderação** (aprovar/reprovar), **painel administrativo com JWT**, **worker** em background e **SignalR** para atualizar o mural quando novas fotos forem aprovadas.

## Visão da arquitetura

| Camada | Projeto |
| --- | --- |
| API | `Backend/Api` — REST, Swagger, JWT, SignalR `/hubs/wall` |
| Worker | `Backend/Worker` — polling periódico (~30s; respeita `PollIntervalMinutes` por hashtag) |
| Domínio | `Backend/Domain` |
| Aplicação | `Backend/Application` — DTOs e interfaces |
| Infraestrutura | `Backend/Infrastructure` — EF Core (PostgreSQL), cliente Graph API |
| Admin | `Frontend/admin` — Vite + React + TypeScript |
| Mural | `Frontend/wall` — Vite + React + TypeScript (fullscreen) |

## Pré-requisitos

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) (para os frontends)
- [Docker](https://www.docker.com/) (opcional, apenas para PostgreSQL via `docker-compose`)

## Banco de dados (PostgreSQL)

Subir apenas o Postgres:

```bash
docker compose up -d postgres
```

A connection string padrão em `Backend/Api/appsettings.json` aponta para `localhost:5432`, banco `hashtagwall`, usuário/senha `postgres`.

## Backend — API

```bash
cd Backend/Api
dotnet run
```

Por padrão (perfil **http**): API em `http://localhost:5233`, Swagger em `/swagger`.

Na primeira execução:

1. Migrações EF são aplicadas automaticamente (`MigrationHostedService`).
2. Um usuário admin inicial é criado (`AdminSeedHostedService`) se ainda não existir.

Credenciais padrão (altere em produção):

- Usuário: `admin`
- Senha: `ChangeMe123!`

Variáveis de ambiente opcionais para seed:

- `SEED_ADMIN_USERNAME`
- `SEED_ADMIN_PASSWORD`

### JWT

Configure um segredo forte em `Jwt:Key` (**mínimo 32 caracteres**) em `appsettings.json` ou por variável de ambiente (`Jwt__Key`).

### CORS

Origens permitidas estão em `Cors:Origins` (`Backend/Api/appsettings.json`): por padrão incluem `http://localhost:5173` (admin) e `http://localhost:5174` (mural).

## Backend — Worker

Executa sincronização com o Instagram conforme intervalo por hashtag (`PollIntervalMinutes`), desde que `IsMonitoringEnabled` esteja `true`.

```bash
cd Backend/Worker
dotnet run
```

Use a **mesma** `ConnectionStrings:DefaultConnection` que a API.

## Frontends

### Variáveis de ambiente

- Admin: `Frontend/admin/.env.development` → `VITE_API_URL=http://localhost:5233`
- Mural: `Frontend/wall/.env.development` → `VITE_API_URL=http://localhost:5233`
- Preview do mural no admin: opcional `VITE_WALL_ORIGIN=http://localhost:5174`

### Admin (painel)

```bash
cd Frontend/admin
npm install
npm run dev
```

Abre em `http://localhost:5173`. Faça login, cadastre hashtag, token Meta e IG User ID, modere posts e abra o preview do mural.

### Mural público

```bash
cd Frontend/wall
npm install
npm run dev
```

URL do mural:

```text
http://localhost:5174/wall/{hashtag}
```

Exemplo (hashtag armazenada sem `#`): `http://localhost:5174/wall/meuevento`

## API REST (resumo)

**Público (sem JWT)**

| Método | Rota | Descrição |
| --- | --- | --- |
| GET | `/api/public/wall/{hashtag}/config` | Configuração visual do mural |
| GET | `/api/public/wall/{hashtag}/approved-posts` | Posts **aprovados**, ordenados por data de postagem |

**Auth**

| Método | Rota | Descrição |
| --- | --- | --- |
| POST | `/api/auth/login` | `{ "username", "password" }` → JWT |

**Admin (Authorization: Bearer)**

| Área | Rotas principais |
| --- | --- |
| Hashtags | `GET/POST /api/admin/hashtags`, `PUT /api/admin/hashtags/{id}` |
| Mural | `GET/PUT /api/admin/hashtags/{id}/wall` |
| Mídia | `GET /api/admin/hashtags/{id}/media`, `POST /api/admin/media/{postId}/approve|reject` |
| Sync manual | `POST /api/admin/hashtags/{id}/sync` |
| Logs | `GET /api/admin/integration-logs?hashtagConfigurationId=&take=` |

**SignalR**

- Hub: `/hubs/wall`
- Evento: `ApprovedPostsUpdated` (após aprovação no painel)
- O mural chama `Subscribe(normalizedHashtag)` ao conectar.

## Instagram Graph API — requisitos e permissões

A API oficial **não** permite scraping; este projeto usa apenas endpoints documentados da Graph API.

### Pré-requisitos de conta

1. Conta **Instagram Profissional** (**Empresa** ou **Criador de conteúdo**).
2. Página do **Facebook** conectada à conta Instagram.
3. App criado no **Meta for Developers**, com produto **Instagram** / uso da **Instagram Graph API**.

### Permissões / escopos usuais (referência)

Os nomes exatos podem variar conforme o tipo de token e fluxo; consulte a documentação atual da Meta. Em geral você precisará de permissões que permitam:

- Ler informações da conta Instagram Business vinculada (`instagram_basic` ou equivalentes atuais).
- Buscar hashtag e mídias recentes conforme política atual da plataforma (endpoints como `ig_hashtag_search` e `recent_media` — sujeitos a disponibilidade e revisão do app).

**Importante:** o endpoint de hashtag tem **limitações** (tipos de conta, escopo do app em modo desenvolvimento vs produção, revisão da Meta). Erros aparecem em **Logs de integração** no admin e nos registros da API.

### IDs necessários no painel

- **Instagram Business Account ID** (`InstagramBusinessAccountId`): o **IG User ID** usado nos parâmetros `user_id` das chamadas à Graph API.
- **Token de acesso**: User/Page token com validade (prefira **long-lived** tokens em produção) — **nunca** exposto no frontend; apenas no backend.

### Fluxo técnico implementado

1. `GET /ig_hashtag_search?user_id={IG_USER_ID}&q={hashtag_sem_#}` → obter `hashtag_id`.
2. `GET /{hashtag_id}/recent_media?user_id={IG_USER_ID}&fields=...` → lista de mídias recentes.

Os itens são salvos como **Pendentes** até aprovação. Duplicidade é evitada pelo par `(HashtagConfigurationId, InstagramMediaId)`.

### Como criar app e token (alto nível)

1. Acesse [Meta for Developers](https://developers.facebook.com/) e crie um **aplicativo**.
2. Adicione o produto **Instagram** (e use **Facebook Login** ou **Business** conforme seu fluxo).
3. Configure **URIs de redirecionamento OAuth** válidos para obter tokens de curta duração e troque por **long-lived** quando aplicável (veja documentação Meta: *Long-Lived Access Tokens*).
4. Associe **conta de teste** / **função** ao app enquanto estiver em modo desenvolvimento.
5. Solicite **revisão do aplicativo** para permissões avançadas antes de uso público amplo.

Para detalhes atualizados de cada passo, use a documentação oficial da Meta para **Instagram Graph API** e **Graph API Explorer**.

### Como testar a integração

1. Suba Postgres, API e Worker.
2. No admin, crie uma hashtag com **IG User ID** e **token** válidos.
3. Use **“Buscar agora”** na moderação ou aguarde o worker.
4. Verifique **Logs de integração** se não aparecerem mídias (permissão, token expirado, app em modo dev, etc.).

## Segurança

- Painel protegido por **JWT**; tokens Meta **somente** no servidor.
- Respostas de configuração de hashtag indicam apenas `metaTokenConfigured` (boolean), não o valor do token.

## Build de produção (frontends)

```bash
cd Frontend/admin && npm run build
cd ../wall && npm run build
```

Saídas em `Frontend/admin/dist` e `Frontend/wall/dist` — sirva via CDN/IIS/nginx ou hospede atrás da mesma origem da API.

## Estrutura de pastas sugerida (entregue)

```text
/Backend
  /Api
  /Application
  /Domain
  /Infrastructure
  /Worker
  (SignalR no projeto Api)

/Frontend
  /admin
  /wall
```

## Licença de uso da Meta / Instagram

Respeite os [Termos da plataforma](https://developers.facebook.com/terms/), limites de taxa, políticas de dados e diretrizes de marca. Este software é apenas um exemplo de integração; **a aprovação do app e o cumprimento das políticas são de responsabilidade do operador**.
