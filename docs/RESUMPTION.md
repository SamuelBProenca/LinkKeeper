# Retomada em Windows — 25/10/2026

## Recuperação e ambiente

Histórico completo recuperado de `linkkeeper-history.bundle`, com integridade
verificada. O ZIP de código confere com o SHA-256
`7138622885462d8d57b1346726dfd2d8fe06356ebad9227bf734d13ef403e88b`.
O remoto possuía apenas `main` em `181dc1f` ao iniciar esta retomada.

Checkout local em `C:\Users\samue\Projetos\LinkKeeper`, separado do Curriculum
Service. Branch de trabalho `codex/resume-validation`, derivada de `25c1c57`.
O VS Code foi aberto nessa pasta. O SDK global encontrado era 9.0.304; o SDK
10.0.402 exigido por `global.json` foi instalado isoladamente em `.tools/dotnet`,
com download da Microsoft e SHA-512 conferido com os metadados oficiais.
SDK, cópias de validação, bancos, senhas e artefatos não são versionados.

O SSH retornou `Permission denied (publickey)`. Criar branch pelo conector
GitHub retornou 403 `Resource not accessible by integration`. A credencial
HTTPS existente no Git local, por outro lado, foi validada com push em modo
`--dry-run`; não foi necessário solicitar token ou chave ao proprietário.

## Correções de comportamento

- Categorias são montadas antes da resolução dos aliases. Alterar uma política
  sem informar `Aliases` preserva os aliases herdados, apontando para a política
  vigente. Uma lista explícita substitui os aliases anteriores; `[]` remove-os.
  Colisões entre categorias e aliases de outras categorias impedem a configuração.
  As políticas históricas continuam iguais na ausência de configuração explícita.
- Parser e persistência compartilham validação de URL HTTP/HTTPS. Chamadas diretas
  a `LinkStore.Add` também verificam categoria configurada, criação obrigatória e
  expiração posterior à criação. Aliases são canonicalizados antes da deduplicação.
  A API injeta o mesmo catálogo na persistência e no parser.
- Consultas aceitam apenas os estados textuais exatos `Active`, `Expired` e
  `Archived`. Números, combinações de enum e espaços extras recebem erro de entrada.
- O smoke HTTP verifica também categoria configurada e rejeição de estado numérico.

## Verificação executada

SDK 10.0.402 em Windows x64. Cada checkpoint foi executado com seu próprio código,
em worktree isolada; os resultados históricos do handoff não foram reutilizados
como prova de execução neste computador.

| Revisão | Testes aprovados | Verificações adicionais |
| --- | ---: | --- |
| `c073efa` — fundação | 23 | Build CLI Release |
| `d714763` — categorias | 25 | Build CLI Release |
| `bdb89ea` — SQLite | 30 | Build CLI Release |
| `25c1c57` — web original | 30 | Builds CLI/Web, smoke HTTP, sintaxe JS, auditoria NuGet |
| Retomada com correções | 57 | Builds CLI/Web, smoke HTTP ampliado, diff sem erros de whitespace |

Antes das correções, 22 dos 25 casos novos iniciais falharam reproduzindo os
problemas; depois, esses casos e dois cenários adicionais passaram. Todos os
builds executados terminaram com zero avisos e zero erros. A auditoria NuGet da Web
incluindo dependências transitivas não encontrou vulnerabilidades conhecidas nas
fontes consultadas na execução da versão original recuperada.

O smoke inicia um processo ASP.NET real com senha aleatória e SQLite temporário.
Verifica autenticação, CSRF, criação, duplicação sem extensão de prazo, busca,
URL/estado inválidos, arquivamento, conflito de renovação de arquivado, categoria
configurada e logout. Ele ainda usa porta fixa 5017 e não substitui teste de navegador.

## Acompanhamento local

As tarefas do VS Code compilam, executam testes, realizam o smoke ou iniciam a web.
A tarefa `LinkKeeper: iniciar web` solicita a senha sem gravá-la em configuração,
escuta somente em `127.0.0.1:5017` e usa banco local persistente fora do Git.
Os comandos de build/teste e o smoke foram executados diretamente; a interação
gráfica com o seletor de tarefas e o prompt de senha não foi automatizada.

## Pendências

A revisão corrigiu os problemas acima e não constitui uma auditoria completa de
todos os cenários do handoff. Ainda faltam testes próprios do loop hospedado do
worker, lock/falha e recuperação do banco, QA visual mobile, corridas da busca no
frontend, categorias dinâmicas no filtro, OpenAPI, validação do container,
backup/restauração de produção e hospedagem HTTPS com persistência e processo ativo.
Não há demo publicada nem integração WhatsApp. CLI com persistência, idempotência
de mensagem externa e autenticação de múltiplas contas também continuam pendentes.

As branches históricas serão apresentadas como PRs encadeados, seguidas pelo PR
da retomada; cada diff deve usar a branch anterior como base. Nenhum merge na
`main` foi autorizado por esta retomada.
