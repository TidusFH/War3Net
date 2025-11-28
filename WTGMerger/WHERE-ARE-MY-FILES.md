# 📁 Onde Colocar Seus Arquivos WTG

## 🎯 Resposta Rápida

O programa lê de **2 pastas** e salva em **1 pasta**:

```
War3Net/
├── WTGMerger/               ← Você está AQUI (onde está o run.bat)
├── Source/                  ← COLOQUE SEU ARQUIVO DE ORIGEM AQUI
│   └── war3map.wtg         (arquivo de onde você quer COPIAR triggers)
└── Target/                  ← COLOQUE SEU ARQUIVO DE DESTINO AQUI
    ├── war3map.wtg         (arquivo PARA ONDE você quer copiar)
    └── war3map_merged.wtg  ← O RESULTADO será salvo AQUI
```

---

## 📝 Passo a Passo (MAIS FÁCIL)

### 1️⃣ Prepare seus arquivos WTG

Você precisa de **2 arquivos `.wtg`**:
- **Source** (origem): O mapa DE ONDE você quer copiar triggers
- **Target** (destino): O mapa PARA ONDE você quer copiar triggers

### 2️⃣ Extraia os arquivos war3map.wtg dos seus mapas

Se você tem arquivos `.w3x` (mapas completos), precisa extrair o `war3map.wtg`:

**Opção A: Usando MPQ Editor**
1. Baixe MPQ Editor
2. Abra seu mapa (.w3x)
3. Procure por `war3map.wtg`
4. Clique com botão direito → Extract

**Opção B: Renomeie .w3x para .zip**
1. Copie seu mapa (ex: `MeuMapa.w3x`)
2. Renomeie para `MeuMapa.zip`
3. Extraia o arquivo
4. Pegue o `war3map.wtg` de dentro

### 3️⃣ Organize os arquivos

```
📂 War3Net-claude-war3-wtg-trigger-merger.../
│
├── 📂 WTGMerger/           ← Pasta do programa
│   ├── run.bat             ← Execute este arquivo
│   └── ...
│
├── 📂 Source/              ← Crie esta pasta se não existir
│   └── war3map.wtg        ← COLOQUE O ARQUIVO DE ORIGEM AQUI
│
└── 📂 Target/              ← Crie esta pasta se não existir
    └── war3map.wtg        ← COLOQUE O ARQUIVO DE DESTINO AQUI
```

### 4️⃣ Execute o programa

```cmd
# Duplo clique em:
WTGMerger/run.bat
```

### 5️⃣ Pegue o resultado

O arquivo mesclado estará em:
```
Target/war3map_merged.wtg
```

---

## 🔧 Método Alternativo: Caminhos Customizados

Se você **NÃO quer** mover seus arquivos para as pastas Source/Target, pode usar caminhos customizados:

### Linha de Comando:

```cmd
cd WTGMerger
dotnet run -- "C:\MeusMaps\MapaA\war3map.wtg" "C:\MeusMaps\MapaB\war3map.wtg" "C:\Desktop\resultado.wtg"
```

**Formato:**
```
dotnet run -- "ORIGEM" "DESTINO" "SAÍDA"
```

### Exemplo Real:

```cmd
dotnet run -- "D:\Warcraft\Maps\RPG\war3map.wtg" "D:\Warcraft\Maps\Defense\war3map.wtg" "D:\Desktop\merged.wtg"
```

---

## 🗺️ Exemplo Completo

### Cenário:
Você tem 2 mapas:
- **DefenseMap.w3x** - Tem triggers de IA que você quer copiar
- **MyMap.w3x** - Seu mapa onde você quer adicionar os triggers

### Passo 1: Extraia os war3map.wtg

```
DefenseMap.w3x → Extrair → war3map.wtg
MyMap.w3x → Extrair → war3map.wtg
```

### Passo 2: Organize

```
War3Net/
├── Source/
│   └── war3map.wtg     ← Do DefenseMap.w3x
└── Target/
    └── war3map.wtg     ← Do MyMap.w3x
```

### Passo 3: Execute

```cmd
# Duplo clique:
WTGMerger/run.bat
```

### Passo 4: Use o menu

```
Select option: 5 (Copy SPECIFIC trigger(s))

Enter category name where the trigger is: AI
Enter trigger name to copy: AI Player 1, AI Player 2
Enter destination category: Custom AI

✓ Trigger(s) copied!

Select option: 6 (Save and exit)
```

### Passo 5: Resultado

```
Target/war3map_merged.wtg  ← SEU ARQUIVO MESCLADO!
```

### Passo 6: Usar no seu mapa

1. Renomeie `war3map_merged.wtg` para `war3map.wtg`
2. Abra `MyMap.w3x` no MPQ Editor
3. Substitua o `war3map.wtg` antigo pelo novo
4. Salve o mapa
5. Pronto! Seus triggers foram copiados!

---

## ❓ Perguntas Frequentes

### P: O programa vai ver meus caminhos?
**R:** Sim! Quando você executar, ele vai mostrar:
```
Using default paths:
  Source: E:\Program\War3Net...\Source\war3map.wtg
  Target: E:\Program\War3Net...\Target\war3map.wtg
  Output: E:\Program\War3Net...\Target\war3map_merged.wtg
```

### P: Posso usar pastas diferentes?
**R:** Sim! Edite o `Program.cs` nas linhas 16-18:
```csharp
var sourcePath = @"C:\MinhaPasta\source.wtg";
var targetPath = @"C:\MinhaPasta\target.wtg";
var outputPath = @"C:\MinhaPasta\output.wtg";
```

### P: E se eu tiver o mapa .w3x completo?
**R:** Você precisa extrair o `war3map.wtg` primeiro usando MPQ Editor ou renomeando para .zip

### P: O arquivo original é modificado?
**R:** NÃO! O programa **NUNCA** modifica os arquivos originais. Ele sempre cria um novo arquivo `war3map_merged.wtg`

### P: Posso usar o mesmo arquivo como source e target?
**R:** Não faz sentido, mas tecnicamente funciona. Use arquivos diferentes!

---

## 🎯 Checklist Rápido

Antes de executar, verifique:

- [ ] Tenho 2 arquivos `war3map.wtg` extraídos
- [ ] Coloquei um na pasta `Source/`
- [ ] Coloquei outro na pasta `Target/`
- [ ] Executei `run.bat` da pasta `WTGMerger/`
- [ ] O programa mostrou os caminhos corretos

Se todos estiverem ✅, você está pronto!

---

## 🚨 Erros Comuns

### Erro: "WTG file not found"
**Causa:** Arquivo não está na pasta certa ou pasta não existe

**Solução:**
1. Verifique se as pastas `Source/` e `Target/` existem
2. Verifique se os arquivos `war3map.wtg` estão dentro delas
3. Execute `run.bat` novamente e veja os caminhos mostrados

### Erro: "Expected file header signature"
**Causa:** O arquivo não é um war3map.wtg válido

**Solução:**
1. Certifique-se de extrair `war3map.wtg` do mapa corretamente
2. Não renomeie outros arquivos para `war3map.wtg`
3. Use apenas arquivos originais do Warcraft III

---

## 💡 Dica Pro

Crie uma estrutura assim para organizar melhor:

```
MeusProjetos/
├── War3Net/
│   └── WTGMerger/
│       └── run.bat
├── Maps/
│   ├── DefenseMap/
│   │   └── war3map.wtg
│   ├── RPGMap/
│   │   └── war3map.wtg
│   └── TowerDefense/
│       └── war3map.wtg
└── Results/
    └── merged_triggers/
```

E use caminhos absolutos:
```cmd
dotnet run -- "C:\MeusProjetos\Maps\DefenseMap\war3map.wtg" "C:\MeusProjetos\Maps\RPGMap\war3map.wtg" "C:\MeusProjetos\Results\merged.wtg"
```

---

**Precisa de ajuda?** Execute `run.bat` e veja os caminhos que o programa está usando!
