# 🔧 Troubleshooting: Múltiplas Pessoas Não Recebem Máscaras

## 🎯 Problema Comum

**Sintoma:** Apenas UMA pessoa recebe máscara, mesmo com múltiplas faces detectadas.

**Causa Raiz:** Todos os PositionTrackers estão seguindo o MESMO FaceObject (geralmente o FaceIndex 0).

---

## ✅ Solução Rápida (Automática)

### Passo 1: Adicionar o Debugger
1. Crie um GameObject vazio na cena
2. Adicione o componente `FaceTrackingDebugger`
3. Execute o jogo
4. Verifique o Console para ver o problema

### Passo 2: Usar a Ferramenta de Auto-Link
1. No Unity Editor, vá em: **Tools → Face Tracking → Auto-Link Position Trackers to FaceObjects**
2. Isso vinculará automaticamente cada PositionTracker a um FaceObject diferente
3. Execute novamente e teste!

### Passo 3: Verificar Configuração
1. No Unity Editor: **Tools → Face Tracking → Verify Setup**
2. Leia o relatório no Console
3. Se houver problemas, eles serão listados

---

## 🔍 Solução Manual (Se a automática não funcionar)

### Estrutura Correta da Cena

Você DEVE ter esta estrutura:

```
Cena
├─ FaceTracker (componente FaceTracker)
│
├─ FaceObject_0 (componente FaceObject, faceIndex = 0)
│  ├─ Wearable_Leonardo
│  ├─ Wearable_Raphael
│  ├─ Wearable_Donatello
│  └─ Wearable_Michelangelo
│
├─ FaceObject_1 (componente FaceObject, faceIndex = 1)
│  ├─ Wearable_Leonardo
│  ├─ Wearable_Raphael
│  ├─ Wearable_Donatello
│  └─ Wearable_Michelangelo
│
├─ FaceObject_2 (componente FaceObject, faceIndex = 2)
│  ├─ Wearable_Leonardo
│  ├─ Wearable_Raphael
│  ├─ Wearable_Donatello
│  └─ Wearable_Michelangelo
│
├─ FaceObject_3 (componente FaceObject, faceIndex = 3)
│  ├─ Wearable_Leonardo
│  ├─ Wearable_Raphael
│  ├─ Wearable_Donatello
│  └─ Wearable_Michelangelo
│
├─ PositionTracker_0 (componente PositionTracker)
│  └─ Target: FaceObject_0 ← IMPORTANTE!
│  └─ Objects To Disable [4]:
│     ├─ [0] Wearable_Leonardo (filho de FaceObject_0)
│     ├─ [1] Wearable_Raphael (filho de FaceObject_0)
│     ├─ [2] Wearable_Donatello (filho de FaceObject_0)
│     └─ [3] Wearable_Michelangelo (filho de FaceObject_0)
│
├─ PositionTracker_1 (componente PositionTracker)
│  └─ Target: FaceObject_1 ← DIFERENTE!
│  └─ Objects To Disable [4]:
│     ├─ [0] Wearable_Leonardo (filho de FaceObject_1)
│     ├─ [1] Wearable_Raphael (filho de FaceObject_1)
│     ├─ [2] Wearable_Donatello (filho de FaceObject_1)
│     └─ [3] Wearable_Michelangelo (filho de FaceObject_1)
│
├─ PositionTracker_2 (componente PositionTracker)
│  └─ Target: FaceObject_2 ← DIFERENTE!
│
├─ PositionTracker_3 (componente PositionTracker)
│  └─ Target: FaceObject_3 ← DIFERENTE!
│
└─ WearableManager (componente WearableManager - criado automaticamente)
```

### Checklist Manual

**1. Verificar FaceObjects:**
- [ ] Existem 4 FaceObjects na cena
- [ ] Cada um tem `faceIndex` único (0, 1, 2, 3)
- [ ] Cada um tem 4 wearables como filhos
- [ ] Todos começam INATIVOS (SetActive = false)

**2. Verificar PositionTrackers:**
- [ ] Existem 4 PositionTrackers na cena
- [ ] Cada PositionTracker tem um `target` DIFERENTE:
  - PositionTracker_0 → FaceObject_0
  - PositionTracker_1 → FaceObject_1
  - PositionTracker_2 → FaceObject_2
  - PositionTracker_3 → FaceObject_3
- [ ] Cada PositionTracker tem array `objectsToDisable` com 4 elementos
- [ ] Os wearables no array são os filhos do FaceObject correspondente

**3. Verificar Ordem dos Wearables:**
- [ ] TODOS os PositionTrackers têm os wearables na MESMA ORDEM:
  ```
  [0] = Leonardo (Azul)
  [1] = Raphael (Vermelho)
  [2] = Donatello (Roxo)
  [3] = Michelangelo (Laranja)
  ```
- [ ] Esta ordem DEVE ser IDÊNTICA em todos os 4 trackers!

**4. Verificar WearableManager:**
- [ ] Existe na cena (pode ser criado automaticamente)
- [ ] Não foi deletado acidentalmente

---

## 📊 Como Funciona o Sistema

### Fluxo Correto:

```
PESSOA 1 DETECTADA
↓
FaceTracker detecta → Ativa FaceObject_0 (faceIndex = 0)
↓
PositionTracker_0 vê que seu target (FaceObject_0) está ativo
↓
PositionTracker_0 pede wearable ao WearableManager
↓
WearableManager retorna índice 2 (ainda não usado)
↓
PositionTracker_0 ativa objectsToDisable[2] (Donatello)
↓
PESSOA 1 RECEBE MÁSCARA ROXA ✓
```

```
PESSOA 2 DETECTADA
↓
FaceTracker detecta → Ativa FaceObject_1 (faceIndex = 1)
↓
PositionTracker_1 vê que seu target (FaceObject_1) está ativo
↓
PositionTracker_1 pede wearable ao WearableManager
↓
WearableManager retorna índice 0 (índice 2 já está em uso)
↓
PositionTracker_1 ativa objectsToDisable[0] (Leonardo)
↓
PESSOA 2 RECEBE MÁSCARA AZUL ✓
```

### Fluxo ERRADO (Bug):

```
PESSOA 1 DETECTADA
↓
FaceTracker detecta → Ativa FaceObject_0
↓
PositionTracker_0 está seguindo FaceObject_0 → MÁSCARA ✓
↓
PositionTracker_1 está seguindo FaceObject_0 → SEM MÁSCARA ✗
PositionTracker_2 está seguindo FaceObject_0 → SEM MÁSCARA ✗
PositionTracker_3 está seguindo FaceObject_0 → SEM MÁSCARA ✗
```

**Problema:** Todos os trackers seguem o mesmo FaceObject!

---

## 🔨 Comandos de Debug Úteis

### No Console do Unity:

Procure por estas mensagens:

**✅ BOM:**
```
[WearableManager] ✓ PositionTracker_0 recebeu wearable index 2
[WearableManager] ✓ PositionTracker_1 recebeu wearable index 0
[WearableManager] Estado atual: 2 trackers ativos | [PositionTracker_0→2] [PositionTracker_1→0]
```

**❌ RUIM:**
```
[PositionTracker] WearableManager não encontrado! Usando sistema antigo
[WearableManager] Não há wearables disponíveis! Todos os 4 wearables estão em uso.
```

---

## 🎮 Teste Rápido

1. **Execute o jogo**
2. **Coloque 2 pessoas na frente da câmera**
3. **Verifique o Console:**
   - Deve aparecer: `[WearableManager] ✓ PositionTracker_X recebeu wearable index Y`
   - Duas vezes (uma para cada pessoa)
4. **Verifique visualmente:**
   - Ambas as pessoas devem ter máscaras DIFERENTES
   - Se tiverem a mesma máscara ou só uma tiver = BUG

---

## 🆘 Se Nada Funcionar

### Cenário 1: Wearables Repetindo
**Problema:** Duas pessoas com a mesma máscara
**Solução:** Verifique a ordem dos wearables no array `objectsToDisable`. Deve ser IDÊNTICA em todos os trackers.

### Cenário 2: Só Uma Pessoa Recebe Máscara
**Problema:** Múltiplas faces detectadas, mas só uma tem máscara
**Solução:**
1. Use: `Tools → Face Tracking → Verify Setup`
2. Procure por: "PROBLEMA: X trackers seguem o mesmo FaceObject"
3. Use: `Tools → Face Tracking → Auto-Link Position Trackers`

### Cenário 3: Nenhuma Pessoa Recebe Máscara
**Problema:** FaceObjects detectados mas sem wearables
**Solução:**
1. Verifique se os wearables estão como FILHOS dos FaceObjects
2. Verifique se o array `objectsToDisable` está preenchido
3. Verifique os logs: `[PositionTracker] X ativou wearable Y`

### Cenário 4: Erro "Todos os wearables estão em uso" mas só há 1 pessoa
**Problema:** Wearables não estão sendo liberados
**Solução:**
1. Adicione `FaceTrackingDebugger` na cena
2. Execute e veja quantos wearables estão realmente em uso
3. Reinicie a cena (vai chamar `ResetAllAssignments`)

---

## 📝 Scripts Úteis Criados

1. **FaceTrackingDebugger.cs** - Adicione em qualquer GameObject para debug em runtime
2. **FaceTrackingSetupHelper.cs** - Ferramentas no Editor (Menu Tools)
3. **WearableManager.cs** - Gerenciador de distribuição única
4. **CameraResolutionLogger.cs** - Monitora resolução da câmera

---

## 🎯 Resultado Final Esperado

Com **2 pessoas** detectadas:
- ✅ Pessoa 1 recebe máscara A (ex: Donatello - Roxa)
- ✅ Pessoa 2 recebe máscara B (ex: Leonardo - Azul)
- ✅ Máscaras são DIFERENTES
- ✅ Console mostra: "2 trackers ativos"

Com **4 pessoas** detectadas:
- ✅ Pessoa 1 recebe máscara A
- ✅ Pessoa 2 recebe máscara B
- ✅ Pessoa 3 recebe máscara C
- ✅ Pessoa 4 recebe máscara D
- ✅ Todas DIFERENTES
- ✅ Console mostra: "4 trackers ativos"

Com **5 pessoas** detectadas:
- ✅ Pessoas 1-4 recebem máscaras
- ⚠ Pessoa 5 NÃO recebe (limite de 4)
- ⚠ Console mostra: "Não há wearables disponíveis!"

---

**Última atualização:** 2025-01-06
**Versão:** 1.0
