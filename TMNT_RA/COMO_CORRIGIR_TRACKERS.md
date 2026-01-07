# 🔧 COMO CORRIGIR: Todos os Trackers Seguindo a Mesma Face

## ❌ SEU PROBLEMA ATUAL:

Você tem **4 PositionTrackers** mas **TODOS** estão com o campo `Target` apontando para o **MESMO** FaceObject.

Resultado: Todas as 4 máscaras ficam empilhadas no mesmo lugar (no Face0).

## ✅ SOLUÇÃO (NO UNITY EDITOR):

### **Passo 1: Encontre seus FaceObjects na Hierarquia**

Na sua cena, você deve ter algo como:
```
Hierarchy:
├─ HeadTrackerObjectHolder (FaceObject, faceIndex = 0)
├─ HeadTrackerObjectHolder (1) (FaceObject, faceIndex = 1)
├─ HeadTrackerObjectHolder (2) (FaceObject, faceIndex = 2)
└─ HeadTrackerObjectHolder (3) (FaceObject, faceIndex = 3)
```

Cada um desses tem um componente `FaceObject` com um `faceIndex` diferente (0, 1, 2, 3).

---

### **Passo 2: Encontre seus PositionTrackers**

Você provavelmente tem 4 PositionTrackers na cena também. Localize-os.

---

### **Passo 3: Configure o Campo `Target` de Cada Tracker**

**IMPORTANTE:** Cada PositionTracker precisa seguir um FaceObject DIFERENTE!

#### **PositionTracker 0:**
1. Selecione o primeiro PositionTracker na Hierarchy
2. No Inspector, procure o campo `Target`
3. Arraste o `HeadTrackerObjectHolder` (o que tem faceIndex = 0)
4. Verifique o array `Objects To Disable [4]`:
   - Deve conter os 4 wearables que são **filhos** deste FaceObject

#### **PositionTracker 1:**
1. Selecione o segundo PositionTracker
2. No Inspector, campo `Target`
3. Arraste o `HeadTrackerObjectHolder (1)` (faceIndex = 1)
4. Verifique `Objects To Disable [4]`:
   - Deve conter os 4 wearables **filhos** deste FaceObject

#### **PositionTracker 2:**
1. Selecione o terceiro PositionTracker
2. Campo `Target` → `HeadTrackerObjectHolder (2)` (faceIndex = 2)
3. `Objects To Disable [4]` → wearables filhos deste FaceObject

#### **PositionTracker 3:**
1. Selecione o quarto PositionTracker
2. Campo `Target` → `HeadTrackerObjectHolder (3)` (faceIndex = 3)
3. `Objects To Disable [4]` → wearables filhos deste FaceObject

---

### **Passo 4: VERIFICAR Configuração**

Use a ferramenta automática:

1. Menu Unity: **Tools → Face Tracking → Verify Setup**
2. Leia o relatório que aparece
3. Deve dizer: "✓ Tudo configurado corretamente!"

Se aparecer **"PROBLEMA: X trackers seguem o mesmo FaceObject"**, significa que você errou no Passo 3.

---

## 📊 Configuração Correta (Resumo Visual):

```
FaceObject: HeadTrackerObjectHolder (faceIndex = 0)
    ├─ SM_Mascara (wearable 0)
    ├─ SM_Mascara (1) (wearable 1)
    ├─ SM_Mascara (2) (wearable 2)
    └─ SM_Mascara (3) (wearable 3)

PositionTracker_0
    └─ Target: HeadTrackerObjectHolder ←
    └─ Objects To Disable [4]:
        [0] SM_Mascara
        [1] SM_Mascara (1)
        [2] SM_Mascara (2)
        [3] SM_Mascara (3)

────────────────────────────────────────────────

FaceObject: HeadTrackerObjectHolder (1) (faceIndex = 1)
    ├─ SM_Mascara (wearable 0)
    ├─ SM_Mascara (1) (wearable 1)
    ├─ SM_Mascara (2) (wearable 2)
    └─ SM_Mascara (3) (wearable 3)

PositionTracker_1
    └─ Target: HeadTrackerObjectHolder (1) ← DIFERENTE!
    └─ Objects To Disable [4]:
        [0] SM_Mascara (filho do HeadTrackerObjectHolder (1))
        [1] SM_Mascara (1) (filho do HeadTrackerObjectHolder (1))
        [2] SM_Mascara (2) (filho do HeadTrackerObjectHolder (1))
        [3] SM_Mascara (3) (filho do HeadTrackerObjectHolder (1))

────────────────────────────────────────────────

... (mesma coisa para os outros 2)
```

---

## 🎮 Depois de Corrigir:

1. **Salve a cena** (Ctrl+S)
2. **Faça o build** novamente
3. **Teste com 2 pessoas**
4. Os logs devem mostrar:
   ```
   [FaceTrackingDebug] Status: Face0=✓ Face1=✓ | Faces ativas: 2 | Trackers ativos: Face0→1 Face1→1
   ```

   ☝️ Isso significa: 1 tracker por face (correto!)

---

## ⚠️ ERRO COMUM:

**Se os logs mostrarem:**
```
[FaceTrackingDebug] Status: Face0=✓ | Faces ativas: 1 | Trackers ativos: Face0→4 ⚠DUPLICADO⚠
```

Significa que os **4 trackers ainda estão seguindo o mesmo FaceObject (Face0)**.

→ Volte ao Passo 3 e configure corretamente!

---

## 🆘 Alternativa: Usar Auto-Link (Mais Fácil)

Se você não quer fazer manualmente:

1. **Tools → Face Tracking → Auto-Link Position Trackers to FaceObjects**
2. Isso vai fazer automaticamente o vínculo correto
3. **Salve a cena**
4. **Build** e teste

---

## 📸 Screenshot de Como Deve Ficar:

**Inspector do PositionTracker_0:**
```
┌─────────────────────────────────────────┐
│ Position Tracker (Script)              │
├─────────────────────────────────────────┤
│ Target: HeadTrackerObjectHolder         │ ← Face 0
│                                         │
│ Objects To Disable         Size: 4     │
│   Element 0: SM_Mascara               │
│   Element 1: SM_Mascara (1)           │
│   Element 2: SM_Mascara (2)           │
│   Element 3: SM_Mascara (3)           │
│                                         │
│ Position Lerp Speed: 8                 │
│ Rotation Lerp Speed: 10                │
└─────────────────────────────────────────┘
```

**Inspector do PositionTracker_1:**
```
┌─────────────────────────────────────────┐
│ Position Tracker (Script)              │
├─────────────────────────────────────────┤
│ Target: HeadTrackerObjectHolder (1)     │ ← Face 1 (DIFERENTE!)
│                                         │
│ Objects To Disable         Size: 4     │
│   Element 0: SM_Mascara               │ ← Filhos do (1)
│   Element 1: SM_Mascara (1)           │
│   Element 2: SM_Mascara (2)           │
│   Element 3: SM_Mascara (3)           │
│                                         │
│ Position Lerp Speed: 8                 │
│ Rotation Lerp Speed: 10                │
└─────────────────────────────────────────┘
```

E assim por diante...

---

**Última atualização:** 2025-01-06
**Versão:** 1.0
