# 🔍 Máscaras Invisíveis - Troubleshooting

## 📊 Situação Atual

Seus logs mostram que o sistema está **FUNCIONANDO PERFEITAMENTE**:

```
✅ 3 pessoas detectadas
✅ 3 wearables diferentes atribuídos (índices 2, 0, 1)
✅ WearableManager funcionando corretamente
✅ PositionTrackers ativando os wearables corretos
```

**MAS** as máscaras não aparecem visualmente! Por quê?

---

## 🎯 Possíveis Causas

### 1. **Renderers Desabilitados**
Os GameObjects das máscaras estão ativos, mas os componentes `Renderer` (MeshRenderer, SkinnedMeshRenderer) estão desabilitados.

### 2. **Materiais Faltando ou Invisíveis**
- Material é `null`
- Material está com alpha = 0 (transparente)
- Shader incorreto

### 3. **Escala Muito Pequena**
As máscaras existem mas são microscópicas (scale < 0.001)

### 4. **Layer Errado**
As máscaras estão em um Layer que a câmera não renderiza

### 5. **Posição Errada**
As máscaras estão muito longe, atrás da câmera, ou fora do campo de visão

---

## ✅ DIAGNÓSTICO (Passo a Passo)

### **Passo 1: Adicione o WearableVisibilityChecker**

1. No Unity Editor, crie um GameObject vazio
2. Adicione o componente `WearableVisibilityChecker`
3. Configure:
   - `Check On Start` = true
   - `Check Every Frame` = true
   - `Check Interval` = 2

### **Passo 2: Build e Teste**

1. Faça o build
2. Abra no navegador
3. Detecte 2-3 pessoas
4. Olhe os logs no Console do navegador

### **Passo 3: Analise os Logs**

Procure por estas mensagens:

#### ✅ **BOM (Máscaras Visíveis):**
```
[PositionTracker] HeadTrackerObjectHolder ativou wearable 0: SM_Mascara (3/3 renderers visíveis)
[WearableVisibility] Ativos agora (3):
  • HeadTrackerObjectHolder[0] SM_Mascara 👁 Visível (3 renderers)
```

#### ❌ **RUIM (Problema Identificado):**
```
[PositionTracker] HeadTrackerObjectHolder ativou wearable 0: SM_Mascara ⚠ SEM RENDERERS!
```
→ O wearable não tem MeshRenderer/SkinnedMeshRenderer

```
[PositionTracker] HeadTrackerObjectHolder ativou wearable 0: SM_Mascara (0/3 renderers visíveis)
[PositionTracker] ⚠ SM_Mascara está ativo mas INVISÍVEL (renderers desabilitados)!
```
→ Os renderers estão desabilitados

```
[WearableVisibility] ⚠ SM_Mascara está no layer Default que NÃO é visível pela câmera!
```
→ Layer incorreto

---

## 🔧 SOLUÇÕES

### **Solução 1: Habilitar Renderers**

Se os logs mostrarem "renderers desabilitados":

1. No Unity Editor, selecione um dos wearables (ex: SM_Mascara)
2. No Inspector, procure por `MeshRenderer` ou `SkinnedMeshRenderer`
3. Marque o checkbox ✓ ao lado do nome do componente
4. Repita para TODOS os 4 wearables de TODOS os 4 FaceObjects (16 total)
5. Salve → Build → Teste

**OU use o comando automático:**
- Adicione `WearableVisibilityChecker` na cena
- No Context Menu dele (botão direito): `Enable All Renderers`

---

### **Solução 2: Verificar Materiais**

1. Selecione o wearable (SM_Mascara)
2. No MeshRenderer/SkinnedMeshRenderer, veja o campo `Materials`
3. Verifique se há materiais atribuídos
4. Se estiver vazio (None), arraste um material
5. Teste o material:
   - Não deve estar transparente (alpha = 0)
   - Shader deve ser adequado (ex: Standard, URP/Lit)

---

### **Solução 3: Verificar Escala**

1. Selecione o wearable
2. No Transform, veja a `Scale`
3. Se estiver muito pequeno (< 0.01), aumente para 1, 1, 1
4. Se o FaceObject também tiver escala pequena, ajuste

**No código:**
- Os logs vão mostrar: "⚠ ESCALA MUITO PEQUENA!"

---

### **Solução 4: Verificar Layer**

1. Selecione o wearable
2. No topo do Inspector, veja o campo `Layer`
3. Deve estar em `Default` ou no mesmo layer que outros objetos visíveis
4. **NÃO** deve estar em layers ignorados pela câmera (ex: UI, IgnoreRaycast)

**Verifique a câmera:**
1. Selecione a Camera principal
2. No Inspector → Camera → `Culling Mask`
3. Certifique-se que o layer dos wearables está marcado ✓

---

### **Solução 5: Verificar Hierarquia**

As máscaras devem ser **filhos diretos** dos FaceObjects:

```
✅ CORRETO:
HeadTrackerObjectHolder (FaceObject)
├─ SM_Mascara          ← Filho direto
├─ SM_Mascara (1)      ← Filho direto
├─ SM_Mascara (2)      ← Filho direto
└─ SM_Mascara (3)      ← Filho direto

❌ ERRADO:
HeadTrackerObjectHolder (FaceObject)
└─ Container
   └─ SM_Mascara       ← Neto (não filho direto)
```

Se estiverem em um container, mova-as para serem filhas diretas.

---

## 📋 Checklist Completo

Execute este checklist para CADA wearable:

- [ ] Wearable é filho direto do FaceObject?
- [ ] Wearable tem componente MeshRenderer ou SkinnedMeshRenderer?
- [ ] Renderer está **habilitado** (checkbox marcado)?
- [ ] Material está atribuído (não está None)?
- [ ] Material não está transparente?
- [ ] Escala está razoável (> 0.01)?
- [ ] Layer está correto (ex: Default)?
- [ ] Layer está no Culling Mask da câmera?

Multiplique por 16 (4 wearables × 4 FaceObjects) = 16 wearables para verificar!

---

## 🎮 Teste Rápido

### No Unity Editor (Antes do Build):

1. **Ative manualmente um wearable:**
   - Hierarchy → HeadTrackerObjectHolder → SM_Mascara
   - Marque o checkbox ao lado do nome para ativá-lo
   - Vá para a Scene View
   - **Você consegue VER a máscara na Scene?**

2. **Se SIM:**
   - Problema é de tracking/atribuição (mas os logs dizem que está ok...)
   - Verifique se está na posição correta

3. **Se NÃO:**
   - Problema é de visibilidade!
   - Siga as soluções acima

---

## 🔍 Logs Esperados Depois de Corrigir

Depois que você corrigir o problema de visibilidade, os logs devem mostrar:

```
[PositionTracker] HeadTrackerObjectHolder ativou wearable 0: SM_Mascara (3/3 renderers visíveis)
[PositionTracker] HeadTrackerObjectHolder (1) ativou wearable 2: SM_Mascara (2) (3/3 renderers visíveis)
[PositionTracker] HeadTrackerObjectHolder (2) ativou wearable 1: SM_Mascara (1) (3/3 renderers visíveis)

[WearableVisibility] Ativos agora (3):
  • HeadTrackerObjectHolder[0] SM_Mascara 👁 Visível (3 renderers)
  • HeadTrackerObjectHolder (1)[2] SM_Mascara (2) 👁 Visível (3 renderers)
  • HeadTrackerObjectHolder (2)[1] SM_Mascara (1) 👁 Visível (3 renderers)
```

---

## 🆘 Se Ainda Não Funcionar

### Teste com Cubo Simples:

1. Crie um cubo (GameObject → 3D Object → Cube)
2. Faça-o filho de um FaceObject
3. Adicione-o ao array `Objects To Disable`
4. Build e teste
5. **O cubo aparece?**
   - **SIM** → Problema é com o modelo 3D das máscaras
   - **NÃO** → Problema é com configuração geral

### Debug Visual Extremo:

Adicione isto temporariamente no `PositionTracker.cs` (depois da linha que ativa o wearable):

```csharp
// DEBUG: Força todos os renderers ON
Renderer[] allRenderers = childToActivate.GetComponentsInChildren<Renderer>(true);
foreach (var r in allRenderers)
{
    r.enabled = true;
    r.gameObject.SetActive(true);
}
Debug.Log($"[DEBUG] Forcei {allRenderers.Length} renderers ON!");
```

---

**Próximo passo:** Adicione o `WearableVisibilityChecker`, faça build, e me envie os logs que aparecerem!

