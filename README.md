# MU Editor Phoenix

**Mu Editor Community Edition** — editor comunitário para Windows, com fontes sob licença MIT.

Projeto: https://github.com/Aldi888/MuPhoenix-Editor

## Baixar e abrir

Baixe o ZIP na seção Releases, extraia e execute **MU Editor Phoenix.exe**. Requer Windows10/11 e .NET Framework4.8. Não precisa instalar Python, navegador ou outro editor. O título da janela é **Mu Editor Community Edition**, sem Season no nome.

Em Projeto, escolha as pastas do servidor e cliente. O Item.xml de Data/Item alimenta nomes, tamanhos e filtros. Em Conexão e preferências, informe o SQL Server e banco. Windows Authentication é o padrão; login SQL também é aceito.

## Premiações: adicionar item ao personagem ou ao baú

1. Selecione a conta/personagem pelas listas.
2. Abra Inventário e baú e escolha a bolsa ou metade do baú de destino.
3. Clique **Adicionar no personagem** ou **Adicionar no baú**.
4. Escolha o item no catálogo visual e configure nível, durabilidade, skill, luck, adicional, excellent e número de cópias.
5. Confira os itens na grade e clique **Salvar inventário e baú**. A conta deve estar desconectada.

Os itens ficam em rascunho até salvar. O editor confere espaço, bordas e sobreposição. Se não couber o lote completo, não adiciona parcialmente. Cópias são itens separados, não quantidade empilhada. Um item novo ainda não salvo pode ser removido por Cancelar item novo.

O adicional é exibido como **+0, +4, +8, +12, +16, +20, +24 e +28**. O código interno0..7 é convertido automaticamente; valores intermediários são recusados.

O serial definitivo só é obtido ao salvar, pelo procedimento **dbo.WZ_GetItemSerial** do próprio servidor, dentro da mesma transação da gravação. Não usa serial aleatório nem copia o serial de outro item. Falha de gravação desfaz itens e contador. Transferências de itens novos entre os painéis mantêm o rascunho e recebem serial apenas no destino final.

Os novos prêmios desta versão são permanentes, com opções básicas. Itens de inventário dedicado Muun não são inseridos na bolsa/baú comum; use ovos/cupons apropriados ou um adaptador específico. Não há entrega online por escrita direta no banco. A integração com sites/eventos remotos e opções avançadas Ancient/Harmony/socket não está implementada nesta versão.

## Outros módulos

- Contas/VIP/moedas: email, bloqueio, nível/vencimento VIP, WCoinC, WCoinP e GoblinPoint.
- Personagens: nível, atributos, pontos, Zen, resets, CtlCode e campos disponíveis de classe, mapa, PK, Ruud e expansões; MasterLevel/MasterPoint quando o registro já existe.
- Inventário e baú: grades com imagens, opções de itens existentes, movimentação nas bolsas liberadas, baú principal/adicional e transferência conjunta.
- Catálogo: filtros por nome, ID, seção e classe, miniaturas e prévia; até600 resultados por filtro.
- Lojas NPC: XML/TabInfo, seleção visual de itens, edição e exportação, preservando atributos e comentários novos.
- Cash Shop: IBSCategory, IBSPackage, IBSProduct, CashShopPackage e CashShopProduct; diagnóstico de vínculos, edição e exportação Client/Server.
- Interface principal em Português, English e Español. Nomes dos itens vêm do catálogo e não são traduzidos pelo editor.

## Cash Shop: editar e aplicar

Veja [o guia](docs/CASH-SHOP.md). A tela mostra nomes de Item.xml, ofertas de IBSProduct, seção/índice e duração sem acrescentar campos aos arquivos nativos. **Como editar / destinos** explica os cinco arquivos. **Exportar cinco arquivos** cria uma saída separada. **Aplicar no projeto** confere os originais, faz backup e grava nos destinos importados do projeto escolhido; não recarrega nem publica na web.

## Persistência e atualização

Projeto, conexão, idioma, busca, conta/personagem, página e tamanho da janela são salvos automaticamente. Reconectar ao abrir pode ser desativado. Para login SQL, Lembrar senha neste Windows usa DPAPI do usuário atual; não salva senha em texto aberto. Em outro computador/usuário Windows, informe a senha novamente.

As preferências da versão anterior são migradas automaticamente para `%LOCALAPPDATA%/MUEditorPhoenix`. Preferências e cópias protegidas são privadas e ficam fora deste repositório e do ZIP. Não publique a pasta do perfil Windows.

**F5 / Atualizar listas** consulta novamente contas e personagens, incluindo personagens recém-criados, sem descartar alterações pendentes. Há até1000 resultados por lista; refine a busca em instalações maiores. Trocar registros ou fechar com rascunho pendente pede confirmação de descarte.

## Arquivos, imagens e compatibilidade

O adaptador inicial foi desenvolvido para MuDevs com inventário de5975 bytes (239×25) e baú de6000 bytes (240×25), incluindo categorias de item com5 bits. Outros emuladores/layouts precisam de adaptador validado. Equipamentos/slots especiais podem ser visualizados e ter suas opções básicas editadas; sua movimentação requer regras específicas.

Escolha uma pasta de imagens no formato `<seção>/<índice>.png`. A pasta Mu Editor/Item pode ser localizada dentro da instalação escolhida. O programa apenas lê essas imagens; não executa nem depende do MuEditor para funcionar. Sem imagens, mostra nomes/IDs. Para identificar itens de socket, lê opcionalmente `Socket - Settings/SocketItemType.xml` ao lado de Item.xml; opções de socket são iniciadas vazias, sem bônus.

Este projeto não distribui Item.xml real, imagens, binários de jogo, bancos, credenciais ou configurações de um servidor. Cada administrador fornece arquivos que tenha direito de usar. Exemplos incluídos são sintéticos.

## Banco e proteção contra perda de dados

Gravações usam conta offline, transação serializável, conferência contra os valores lidos e cópia local protegida. Alteração concorrente é recusada. Transferência entre inventário/baú é salva em uma transação. Permissões SQL dependem dos módulos usados: SELECT/UPDATE, INSERT em CashShopData se necessário e, para premiações, EXECUTE em WZ_GetItemSerial e leitura de GameServerInfo. Não há configuração de acesso do servidor incluída no programa.

## Compilar, testar e contribuir

```powershell
.\scripts\Build.ps1 -Test
# Integração SQL opcional: cria e remove uma base temporária própria.
.\scripts\Test-Database.ps1 -Server localhost
.\scripts\Package.ps1 -Version 1.3.0
```

Usa o compilador do .NET Framework do Windows, sem pacotes externos. Os testes padrão são sintéticos. A integração SQL usa somente uma base isolada com registros artificiais; não executa premiação em banco do jogo. Veja docs/FORMAT-MUDEVS25.md.

As melhorias devem ser feitas neste repositório e validadas antes de publicar. Issues e pull requests são bem-vindos. Não envie dumps, senhas, imagens proprietárias ou dados pessoais em issues/anexos. O workflow Windows compila e testa cada alteração enviada.

Licença MIT. O executável comunitário não possui certificado comercial de assinatura. Não há telemetria, login de fabricante ou serviço em segundo plano.
