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

## Acompanhar no VS Code

Abra esta pasta e use **Terminal → Executar Tarefa**:

- `LinkKeeper: compilar web` (também Ctrl+Shift+B).
- `LinkKeeper: testar` para testes unitários e de integração SQLite.
- `LinkKeeper: smoke HTTP` para um servidor temporário com dados sintéticos.
- `LinkKeeper: iniciar web` para abrir o servidor em http://127.0.0.1:5017.

A tarefa de início solicita uma senha de pelo menos 16 caracteres, usada somente
no ambiente do processo. Entre no navegador com a mesma senha. O banco local
`linkkeeper.db` fica na raiz, fora do Git, e continua existindo após encerrar o servidor.
Interrompa a tarefa antes de executar o smoke, pois ambos usam a porta 5017.
Esse endereço local atende ao desenvolvimento no computador; o deploy HTTPS
para uso pelo celular continua pendente.

Neste checkout Windows, o SDK 10.0.402 foi instalado isoladamente em `.tools/dotnet`.
As tarefas e novos terminais do VS Code usam esse SDK. A instalação global permanece
separada. Em outro computador Windows, prepare o mesmo SDK nesse diretório ou ajuste
`windows.command` das tarefas e `terminal.integrated.env.windows` para a instalação local.
No Linux/macOS as tarefas usam `dotnet` do PATH e exigem um SDK compatível com `global.json`.

Veja `docs/RESUMPTION.md` para o registro da revisão e as pendências.
