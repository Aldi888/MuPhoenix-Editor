# Perfil MuDevs25

Adaptador explícito; não tratar outros layouts como equivalentes por terem o mesmo tamanho.

- Item:25 bytes. Inventário:239 registros. Registro de baú:240.
- ID: byte0 + ((byte7 &128)<<1) + ((byte9 &240)<<5) + ((byte9 &1)<<13). O último bit permite as categorias16..31. Flags de período nos outros bits baixos de byte9 não integram o ID.
- Nível: bits3..6 do byte1; skill: bit7; luck: bit2.
- Adicional interno: (byte1&3)+((byte7&64)>>4). Interface física: código×4, até28.
- Excellent: bits0..5 do byte7. Serial: bytes3..6 em ordem big-endian.
- Novos itens básicos usam Set/380/Harmony vazios; bytes11..18=FF e bytes19..24=0. Para item de socket identificado pelo arquivo de tipos, byte10=FF (sem bônus). Itens existentes não têm seus campos avançados reconstruídos.

Esses campos e padrões foram confrontados com dados do perfil S20 atual, incluindo registros de categorias estendidas. Não foram importados arquivos de outras Seasons nem copiados recursos dos programas usados como referência de interface. Nenhum registro real de personagem faz parte dos exemplos/testes publicados.

## Seriais e atomicidade

O rascunho de item novo tem serial0 e marcador próprio em memória. O marcador acompanha a movimentação entre grades; alterar opções não o remove. Cancelar antes de salvar não consome serial.

Ao salvar: conferir conta offline e valores originais; bloquear o contador nativo; exigir um único registro em GameServerInfo; executar dbo.WZ_GetItemSerial dentro da transação externa; verificar serial positivo, distinto e transação ainda aberta; gravar todos os registros; confirmar. Não atualizar o contador manualmente nem gerar serial aleatório. Se a gravação falha, a transação desfaz o contador e os itens; o rascunho permanece disponível para correção/repetição.

O adaptador pressupõe a versão nativa do procedimento que mantém a transação externa aberta. Um procedimento diferente deve ser validado antes de habilitar premiações. O editor não redefine nem tenta reparar contadores de um servidor.

## Testes

Testes puros cobrem adicional, IDs estendidos, vetor de serial, padrões de25 bytes, metadados, espaço, rastreamento, edição/cancelamento e título. Testes SQL criam sua própria base descartável e verificam inventário/baú, rollback tardio do serial, nova tentativa sem duplicação, conta online e premiações concorrentes com seriais distintos. Eles não substituem a validação em jogo com a build específica do administrador.
