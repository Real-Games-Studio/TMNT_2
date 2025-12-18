# Sistema de Wearables Únicos - Documentação

## 📋 Visão Geral

Este sistema garante que **até 4 pessoas** possam usar wearables **sem repetição** entre elas. Cada pessoa terá um wearable único dos 4 disponíveis.

## 🎯 Como Funciona

### Componentes Principais

1. **WearableManager** (Singleton)
   - Gerencia a distribuição de wearables
   - Garante que não haja repetição entre trackers
   - Suporta até 4 pessoas com 4 wearables únicos

2. **PositionTracker** (Modificado)
   - Solicita um wearable único ao WearableManager
   - Libera o wearable quando o tracking é perdido
   - Fallback para o sistema antigo se o Manager não existir

3. **ScreenVestiario** (Modificado)
   - Reseta todas as atribuições ao iniciar a tela
   - Garante que cada sessão comece do zero

## 🔧 Configuração no Unity

### Passo 1: Criar o WearableManager
O WearableManager é criado automaticamente como Singleton. Você pode:
- Deixar criar automaticamente (recomendado)
- Ou adicionar manualmente: GameObject vazio → Add Component → WearableManager

### Passo 2: Configurar PositionTrackers
Cada PositionTracker deve ter **exatamente 4 wearables** no array `Objects To Disable`:
```
PositionTracker (para Pessoa 1)
├─ Objects To Disable [4]
│  ├─ [0] Wearable_Leonardo (Máscara Azul)
│  ├─ [1] Wearable_Raphael (Máscara Vermelha)
│  ├─ [2] Wearable_Donatello (Máscara Roxa)
│  └─ [3] Wearable_Michelangelo (Máscara Laranja)

PositionTracker (para Pessoa 2)
├─ Objects To Disable [4]
│  ├─ [0] Wearable_Leonardo (Máscara Azul)
│  ├─ [1] Wearable_Raphael (Máscara Vermelha)
│  ├─ [2] Wearable_Donatello (Máscara Roxa)
│  └─ [3] Wearable_Michelangelo (Máscara Laranja)

... (até 4 pessoas)
```

**IMPORTANTE:** Todos os PositionTrackers devem ter os wearables na **mesma ordem** nos índices correspondentes!

## 🎮 Comportamento do Sistema

### Quando uma pessoa é detectada:
1. O PositionTracker solicita um índice único ao WearableManager
2. O Manager retorna um índice de 0-3 que ainda não está em uso
3. O wearable correspondente ao índice é ativado
4. Esse índice fica "reservado" para essa pessoa

### Quando a pessoa perde o tracking:
1. O PositionTracker libera o índice
2. O índice volta para a pool de disponíveis
3. Próxima pessoa que for detectada pode usar esse índice

### Exemplo Prático:
```
Pessoa 1 detectada → Recebe índice 2 (Donatello - Roxa)
Pessoa 2 detectada → Recebe índice 0 (Leonardo - Azul)
Pessoa 3 detectada → Recebe índice 3 (Michelangelo - Laranja)
Pessoa 4 detectada → Recebe índice 1 (Raphael - Vermelha)
Pessoa 5 detectada → ⚠ AVISO: Todos os wearables estão em uso!

Pessoa 1 perde tracking → Índice 2 liberado
Pessoa 5 detectada → Recebe índice 2 (Donatello - Roxa)
```

## 📊 Logs e Debug

O sistema gera logs detalhados para facilitar debug:

```
[WearableManager] ✓ PositionTracker_1 recebeu wearable index 2 (Disponíveis: 2)
[WearableManager] Estado atual: 2 trackers ativos | [PositionTracker_1→2] [PositionTracker_2→0]
[PositionTracker] PositionTracker_1 ativou wearable 2: Wearable_Donatello
```

## 🎬 Fluxo Completo

### Início da Tela (ScreenVestiario)
```
1. SetupScreen() é chamado
2. WearableManager.ResetAllAssignments()
   → Todos os índices (0-3) ficam disponíveis
```

### Detecção de Face
```
1. IsFaceTracked() retorna true
2. PositionTracker.ActivateRandomChild()
3. WearableManager.AssignWearableIndex(tracker)
   → Retorna índice único
4. Wearable correspondente é ativado
```

### Perda de Tracking
```
1. PositionTracker.StopTracking()
2. WearableManager.ReleaseWearableIndex(tracker)
   → Índice volta para disponíveis
3. Todos os wearables do tracker são desativados
```

## ⚠️ Limitações

- **Máximo de 4 pessoas simultâneas** (hardcoded)
- Todos os PositionTrackers devem ter **exatamente 4 wearables**
- Os wearables devem estar na **mesma ordem** em todos os trackers
- Se uma 5ª pessoa for detectada, ela não receberá wearable

## 🔍 Troubleshooting

### "Não há wearables disponíveis! Todos os 4 wearables estão em uso."
- **Causa:** 4 pessoas já estão usando wearables
- **Solução:** Normal, é o limite do sistema

### "WearableManager não encontrado! Usando sistema antigo"
- **Causa:** WearableManager não foi criado
- **Solução:** O Manager é criado automaticamente, verifique se não foi deletado

### "recebeu índice 3 mas só tem 2 wearables!"
- **Causa:** Array de wearables não tem 4 elementos
- **Solução:** Adicione wearables até ter exatamente 4

### Wearables estão repetindo entre pessoas
- **Causa:** Ordem diferente nos arrays dos trackers
- **Solução:** Verifique se todos os trackers têm os mesmos wearables nas mesmas posições

## 🎨 Personalização

### Mudar a Quantidade de Wearables
Para suportar mais ou menos wearables, edite em `WearableManager.cs`:
```csharp
// Linha 26: Mudar de {0,1,2,3} para a quantidade desejada
private List<int> availableIndices = new List<int> { 0, 1, 2, 3, 4, 5 }; // 6 wearables
```

**IMPORTANTE:** Ajuste também os arrays em todos os PositionTrackers!

## 📸 Resolução da Câmera

### Componentes de Monitoramento

#### CameraResolutionLogger
- **Onde adicionar:** No GameObject que tem o componente `ARCamera`
- **O que faz:** Loga a resolução da câmera quando o evento OnResized é disparado
- **Log formato:**
  ```
  ╔═══════════════════════════════════════════════════════════╗
  ║ 📷 RESOLUÇÃO DA CÂMERA
  ╠═══════════════════════════════════════════════════════════╣
  ║ Resolução: 1920 x 1080 pixels
  ║ Megapixels: 2.07 MP
  ║ Aspect Ratio: 16:9 (Widescreen)
  ║ Orientação: Landscape (Horizontal)
  ╚═══════════════════════════════════════════════════════════╝
  ```

#### WebCamInfo
- **Onde adicionar:** Em qualquer GameObject (ex: GameObject vazio chamado "WebCamDebug")
- **O que faz:**
  - No Editor/Standalone: Lista todas as webcams detectadas e testa resoluções suportadas
  - No WebGL: Avisa que a resolução é controlada pelo navegador
- **Settings:**
  - `Log On Start`: Loga automaticamente ao iniciar
  - `Log Detailed Info`: Testa resoluções suportadas (pode demorar um pouco)

### Como Usar
1. Adicione `CameraResolutionLogger` no GameObject do ARCamera
2. Adicione `WebCamInfo` em qualquer GameObject
3. Execute o projeto
4. Verifique o Console para ver os logs

### Resoluções Máximas Comuns
- **VGA:** 640x480 (0.3 MP)
- **720p HD:** 1280x720 (0.9 MP)
- **1080p Full HD:** 1920x1080 (2.1 MP)
- **1440p 2K:** 2560x1440 (3.7 MP)
- **4K UHD:** 3840x2160 (8.3 MP)

**NOTA:** No WebGL, a resolução final depende:
- Resolução nativa da webcam
- Configurações do navegador
- Permissões concedidas pelo usuário
- Configurações do site (constraints da MediaStream API)

---

**Última atualização:** 2025-12-18
**Versão:** 1.0
