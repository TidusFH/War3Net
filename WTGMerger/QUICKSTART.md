# WTGMerger - Guia Rápido de Uso

## ⚠ Problema: Tela Azul "This app can't run on your PC"

Se você está vendo uma tela azul do Windows dizendo **"This app can't run on your PC"**, isso acontece porque:

1. O .exe não é compatível com sua arquitetura do Windows (x64 vs x86)
2. Você está tentando executar um .dll ao invés de um .exe
3. O executável requer o .NET Runtime que não está instalado

### ✅ SOLUÇÃO MAIS FÁCIL: Use o Script BAT (Não precisa de .exe!)

**Você NÃO precisa de um .exe!** Use os scripts .bat que funcionam diretamente:

---

## ✅ Solução 1: Use o Script BAT (MAIS FÁCIL)

### Opção A: Modo Simples
1. **Duplo clique** em `run.bat`
2. O script vai:
   - Verificar se o .NET está instalado
   - Compilar o projeto automaticamente
   - Executar o programa

### Opção B: Modo Customizado (Arrastar e Soltar)
1. **Duplo clique** em `merge-triggers.bat`
2. Arraste e solte (ou cole o caminho) dos arquivos:
   - Arquivo SOURCE (de onde copiar)
   - Arquivo TARGET (para onde copiar)
   - Arquivo OUTPUT (onde salvar o resultado)

---

## ✅ Solução 2: Instalar o .NET Runtime

Se o script BAT não funcionar, você precisa instalar o .NET:

1. **Baixe o .NET 8.0 Runtime:**
   - Acesse: https://dotnet.microsoft.com/download/dotnet/8.0
   - Baixe: **.NET Desktop Runtime 8.0** (Windows x64)

2. **Instale o .NET**
   - Execute o instalador
   - Siga as instruções

3. **Tente novamente:**
   ```cmd
   dotnet run
   ```

---

## ✅ Solução 3: Criar um EXE Standalone (Para Windows 10)

Se você REALMENTE quer um .exe que funcione sem instalar o .NET:

### Opção A: Use o Script Automático
```cmd
# Duplo clique em:
build-exe.bat
```

### Opção B: Linha de Comando
```cmd
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Isso vai criar um arquivo `.exe` em:
```
bin\Release\net8.0\win-x64\publish\WTGMerger.exe
```

⚠️ **IMPORTANTE**: O arquivo terá ~70-100MB porque inclui o .NET Runtime inteiro.

Você pode copiar este arquivo para qualquer lugar (incluindo máquinas sem .NET) e executá-lo diretamente.

---

## 📋 Uso Passo a Passo

### Método 1: Usando o Script BAT

```cmd
# Duplo clique em run.bat
# OU execute no terminal:
run.bat
```

### Método 2: Linha de Comando (Padrão)

```cmd
cd WTGMerger
dotnet run
```

### Método 3: Linha de Comando (Caminhos Personalizados)

```cmd
dotnet run -- "C:\Maps\Source\war3map.wtg" "C:\Maps\Target\war3map.wtg" "C:\Output\merged.wtg"
```

### Método 4: Executar o EXE (Depois de Publicar)

```cmd
.\bin\Release\net8.0\win-x64\publish\WTGMerger.exe
```

Ou com argumentos:
```cmd
WTGMerger.exe "C:\path\to\source.wtg" "C:\path\to\target.wtg" "C:\path\to\output.wtg"
```

---

## 🎮 Novas Funcionalidades (Menu Interativo)

O programa agora oferece um **menu interativo completo** com as seguintes opções:

### **Menu Principal:**
```
1. List all categories from SOURCE      - Ver todas as categorias do arquivo de origem
2. List all categories from TARGET      - Ver todas as categorias do arquivo de destino
3. List triggers in a specific category - Listar triggers dentro de uma categoria
4. Copy ENTIRE category                 - Copiar categoria INTEIRA
5. Copy SPECIFIC trigger(s)             - Copiar APENAS triggers específicos
6. Save and exit                        - Salvar e sair
7. Exit without saving                  - Sair sem salvar
```

### **Exemplo de Uso:**

#### Copiar Triggers Específicos (NOVO!)
```
Select option: 5

Enter category name where the trigger is: AI
  Triggers in 'AI': 5

  [1] AI Player 1
      Enabled: True
      Events: 1
      Conditions: 0
      Actions: 5

  [2] AI Player 2
      Enabled: True
      Events: 1
      Conditions: 0
      Actions: 5

Enter trigger name to copy (or multiple separated by comma): AI Player 1, AI Player 2

Enter destination category name (leave empty to keep same): Custom AI

  ✓ Created new category 'Custom AI'

  Copying 2 trigger(s) to category 'Custom AI':
    ✓ AI Player 1
    ✓ AI Player 2
```

#### Copiar Categoria Inteira
```
Select option: 4

Enter category name to copy: Melee Initialization

Merging category 'Melee Initialization' from source to target...
  Found 12 triggers in source category
  Added category 'Melee Initialization' to target
    + Copied trigger: Melee Game Init
    + Copied trigger: Melee Starting Resources
    + ...
✓ Category copied!
```

### **Recursos:**
- ✅ **Copiar triggers individuais** - Não precisa copiar a categoria inteira!
- ✅ **Copiar múltiplos triggers de uma vez** - Separe por vírgula
- ✅ **Escolher categoria de destino diferente** - Organize como quiser
- ✅ **Ver informações detalhadas** - Events, Conditions, Actions de cada trigger
- ✅ **Salvar apenas quando quiser** - Faça várias operações antes de salvar

---

## 🔧 Troubleshooting

### Erro: "dotnet: command not found"
**Solução:** Instale o .NET 8.0 SDK
- https://dotnet.microsoft.com/download/dotnet/8.0

### Erro: "Could not find internal MapTriggers constructor"
**Solução:** As DLLs estão corrompidas ou incorretas
- Verifique que as DLLs em `../Libs/` são válidas
- Recompile o War3Net do código fonte se necessário

### Erro: "WTG file not found"
**Solução:** Caminho do arquivo está errado
- Use caminhos absolutos: `C:\Full\Path\To\war3map.wtg`
- OU coloque os arquivos nas pastas esperadas:
  - `../Source/war3map.wtg`
  - `../Target/war3map.wtg`

### Erro: "Expected file header signature"
**Solução:** O arquivo não é um WTG válido
- Certifique-se de que é um arquivo `war3map.wtg`
- Extraia de um mapa .w3x válido usando MPQ Editor

---

## 📝 Exemplo Completo

```cmd
# 1. Abra o Prompt de Comando (cmd)
# 2. Navegue até a pasta do projeto
cd E:\Program\War3Net\WTGMerger

# 3. Execute o programa
dotnet run

# O programa vai perguntar:
Enter category name to copy: AI

# Digite o nome da categoria e pressione Enter
# O resultado será salvo automaticamente!
```

---

## 🎯 Quick Commands

```cmd
# Compilar apenas
dotnet build

# Executar (modo debug)
dotnet run

# Executar (modo release, mais rápido)
dotnet run --configuration Release

# Criar executável standalone
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Executar com caminhos customizados
dotnet run -- "source.wtg" "target.wtg" "output.wtg"
```

---

## 📦 Estrutura de Arquivos

```
WTGMerger/
├── Program.cs              # Código principal
├── WTGMerger.csproj        # Configuração do projeto
├── run.bat                 # Script fácil de usar
├── merge-triggers.bat      # Script com caminhos customizados
├── README.md               # Documentação completa
└── QUICKSTART.md           # Este guia (você está aqui!)
```

---

## ❓ Ainda com Problemas?

Se nada funcionar:

1. **Verifique se tem .NET instalado:**
   ```cmd
   dotnet --version
   ```
   Deve mostrar: `8.0.x` ou similar

2. **Tente compilar manualmente:**
   ```cmd
   dotnet build
   ```
   Veja se aparecem erros

3. **Verifique as DLLs:**
   - Certifique-se de que `../Libs/War3Net.Build.Core.dll` existe
   - Certifique-se de que `../Libs/War3Net.Build.dll` existe
   - Certifique-se de que `../Libs/War3Net.Common.dll` existe

4. **Use caminhos absolutos completos** ao invés de relativos

---

## 🎉 Pronto!

Agora você pode facilmente mesclar triggers entre mapas do Warcraft 3!
