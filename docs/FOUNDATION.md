# Auditoria e fundação

Estado observado em 24/10/2026 UTC: repositório público, main com apenas README;
issue #1 setup sem corpo. Nenhum código legado disponível. O proprietário autorizou
iniciar do zero. Não há comportamento antigo executável para caracterizar.

Primeiro incremento: biblioteca Core sem dependências de produção, parser inicial,
CLI de validação, testes determinísticos e CI. Não representa MVP completo.
.NET 10 escolhido por ser LTS suportado:
https://dotnet.microsoft.com/en-us/platform/support/policy

Arquitetura alvo: monólito modular. Domain e Application dentro de Core inicialmente;
Infrastructure para banco/worker; CLI e ASP.NET Core como apresentações. Criar módulos
quando houver implementação, sem projetos vazios. Banco local SQLite com migrations;
PostgreSQL avaliado na hospedagem. Sem Redis, broker ou microsserviços.

Próximos PRs, dependentes deste e sem merge automático:
1. Domínio LinkItem, políticas configuráveis, URL normalizada e ciclo de vida em memória.
2. Persistência SQLite, migrations, isolamento por usuário, deduplicação e consultas.
3. Worker transacional e idempotente, concorrência e recuperação.
4. API autenticada, limites e OpenAPI.
5. WhatsApp oficial: assinatura, identidade, idempotência e confirmações com retries.
6. Deploy, backups, health e validação operacional.

Não implementados: cadastro persistente, consultas, renovação, arquivamento,
expiração automatizada, autenticação, API ou WhatsApp. Nenhum segredo é necessário
para validar o parser. Integração real exige credenciais; simulações serão identificadas.

## Uso de IA

Adaptamos as recomendações de tarefas pequenas, restrições explícitas, revisão do diff,
verificação no commit exato e regressões por falha. Não instalamos skills de terceiros.
Fontes lidas:
https://ailair.akitaonrails.com/pt-br/workflow/
https://akitaonrails.com/2026/09/17/falando-um-pouco-sobre-minhas-skills-de-ia/
