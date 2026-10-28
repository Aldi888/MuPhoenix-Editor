# Cash Shop

Abra as pastas corretas de servidor e cliente no Projeto. Importe Item.xml para identificar os itens. A versão ativa dos IBS é escolhida pela configuração do seu servidor; não escolha uma pasta só por ser a de número maior.

| Arquivo | Destino | Papel |
|---|---|---|
| CashShopPackage.txt | Servidor: Data/CashShop | Pacotes, moedas, preços e vínculos |
| CashShopProduct.txt | Servidor: Data/CashShop | Item, opções, quantidade de entrega e duração |
| IBSCategory.txt | Cliente: Data/InGameShopScript/versão ativa | Categorias |
| IBSPackage.txt | Mesmo diretório do cliente | Ofertas, preços e apresentação |
| IBSProduct.txt | Mesmo diretório do cliente | Variantes, nomes e propriedades |

ProductBase agrupa variantes; ProductMain identifica uma opção de compra. Não são o ID do item. ItemCode = Section × 512 + Index; por exemplo, 6720 = 13:64. O nome-base vem de Item.xml; a oferta vem de IBSProduct pelo par ProductID/PriceID. A coluna Duração converte os segundos do servidor. As colunas auxiliares são apenas visuais e não entram na exportação.

Quantity não é contador de estoque. Zero não prova que uma oferta está esgotada; itens por duração têm também parâmetros em IBSProduct. Não normalize zeros indiscriminadamente. Validar catálogo mostra ofertas do cliente sem contraparte no servidor, vendas desabilitadas e vínculos inválidos. A validação não inventa pacotes/preços nem garante compatibilidade com todo emulador.

Para alterar preço de pacote simples, use Sincronizar preço simples com MainID/PackageID. Isso mantém os quatro arquivos vinculados. Bundles/variantes e produtos compartilhados precisam de revisão manual. Revise os diagnósticos antes de aplicar.

Exportar cria Client/ e Server/, LEIA-ME.txt, DIAGNOSTICO.txt e DESTINOS.txt, sem modificar as instalações. Aplicar no projeto confere que os cinco originais pertencem às pastas escolhidas e não mudaram desde a leitura; recusa redirecionamentos de pasta. Salva os originais em %LOCALAPPDATA%/MUEditorPhoenix/CashShopBackups, com o mapa de destinos. Uma falha de gravação tenta desfazer as gravações concluídas. Não existe transação única entre cinco arquivos; uma mudança externa durante a reversão exige restauração manual a partir do backup.

Aplicar pede revisão dos caminhos e da quantidade de divergências. Divergências existentes não são corrigidas automaticamente nem impedem uma aplicação conscientemente confirmada. A serialização e os formatos/números do servidor são conferidos antes de gravar. Para desfazer, copie os originais do backup para seus destinos e recarregue CashShop.

Depois: recarregue CashShop nos GameServers que usam esses arquivos e reabra o cliente. Canais com Data separados são projetos separados. O editor não reinicia processos.

Distribua aos jogadores apenas os três IBS, nos mesmos caminhos relativos. Use o publicador do seu launcher para criar uma nova versão, hashes e manifesto assinado, além de atualizar o pacote completo. Não sobrescreva uma versão publicada sem refazer o manifesto. As pastas web dependem da instalação de cada comunidade; credenciais e chaves privadas nunca pertencem ao editor público.
