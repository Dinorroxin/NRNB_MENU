# .BAT

Este arquivo .bat serve para publicar o projeto Hud_Principal e copiar o .exe para outro diretório.

**Caso** seja realizado o publish sem os passos informados Dependencies.md, o .exe não funcionará corretamente. O `.bat` não copia `captcha_solver.exe` nem `gal_extract.exe` diretamente. 
Essa cópia é feita pelo `dotnet publish` (via configuração no `.csproj`), desde que os arquivos já estejam na raiz do projeto — ver Dependencies.md. O robocopy só espelha o resultado do publish para o destino final.

```
set PROJETO=C:{Caminho abosluto até o diretório}\NRNB_MENU\Hud_Principal\Hud_Principal.csproj
set DESTINO=C:{Caminho absoluto até onde você quer que o .exe fique}

echo === Publicando ===
dotnet publish %PROJETO% -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish_temp

echo === Copiando ===
robocopy publish_temp %DESTINO% /MIR /XF config.json

echo === Concluido ===
pause
```

---