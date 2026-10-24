# LinkKeeper

Organizador de links em desenvolvimento, iniciado do zero com autorização do proprietário.
Este incremento oferece somente validação de comandos em CLI. Não salva links.

Requer SDK .NET 10 (LTS).

```sh
dotnet build src/LinkKeeper.Cli -c Release
dotnet test tests/LinkKeeper.Tests -c Release
dotnet run --project src/LinkKeeper.Cli -- https://example.com --jobs /7dias
```

A saída JSON informa `validation-only`. Erros retornam código 2.
Categorias iniciais: jobs (30 dias), fun/art (permanentes); vaga-emprego é alias de jobs.
Outras categorias e suas políticas configuráveis serão adicionadas no incremento do domínio.

Veja docs/FOUNDATION.md para auditoria, arquitetura e sequência dos PRs.
