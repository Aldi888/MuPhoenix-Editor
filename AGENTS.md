# MU Editor Phoenix

Projeto público do editor. Título da janela: Mu Editor Community Edition. Fontes autoritativas neste repositório; futuras melhorias devem ser feitas aqui e enviadas ao remoto após validação.

Windows/.NET Framework 4.8, WinForms, sem pacotes externos. Execute scripts/Build.ps1 e scripts/Test.ps1. Testes SQL devem usar somente a base temporária própria; nunca gerar prêmio em conta real como teste.

Não adicionar arquivos de servidor/cliente reais, imagens de terceiros, credenciais, preferências locais, bancos, backups ou evidências privadas ao Git. Exemplos devem ser sintéticos.

O perfil MuDevs usa registros de25 bytes, inventário239 slots e baú240. Adicional na interface0..28 em passos de4; armazenamento0..7. Novos itens só recebem serial pelo WZ_GetItemSerial na mesma transação da gravação, após verificar conta offline. Não usar números aleatórios nem executar esse procedimento em produção para testes. Preservar a migração das preferências da versão anterior.
