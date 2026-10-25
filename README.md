# LinkKeeper

Organizador pessoal de links em C#/.NET 10, em desenvolvimento.

## Estado atual

CLI para validar comandos; versão web local com login, cadastro persistente SQLite,
listagem, busca, filtros, arquivamento, histórico, renovação e expiração periódica.
Ainda sem deploy acessível, WhatsApp ou cadastro de múltiplos usuários.

```sh
dotnet build src/LinkKeeper.Web -c Release
dotnet test tests/LinkKeeper.Tests -c Release
python3 scripts/smoke-web.py
dotnet run --project src/LinkKeeper.Cli -- https://example.com --jobs /7dias
```

Para iniciar a web, configure LINKKEEPER_PASSWORD (segredo de pelo menos 16 caracteres)
e LINKKEEPER_DB (caminho absoluto de banco em disco persistente), então execute:

```sh
dotnet run --project src/LinkKeeper.Web
```

Use HTTPS em produção. Em desenvolvimento local HTTP, defina ASPNETCORE_ENVIRONMENT=Development.
A senha configura o acesso à conta pessoal; não existe inscrição pública de usuários.
Nunca publique senhas em arquivos versionados. A CLI ainda não grava no banco.

Categorias: jobs (30 dias), fun/art (permanentes); vaga-emprego é alias de jobs.
video/estudo/ler-depois exigem duração explícita por padrão; políticas personalizadas
são configuráveis no servidor. Comandos aceitam /7d, /7dias, /30d e /permanente.
Uma URL duplicada na mesma categoria retorna o registro existente sem estender prazo.

Veja docs/WEB-STATUS.md para validação executada, limitações e hospedagem.
