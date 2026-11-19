# VERIFY kasıtlı bozmaları: repo DIŞINDA bir kopyada, her bozma kendi temiz kopyasında.
set -u
SRC="C:/Dosyalar/Projects/engin_demirog/anewcoursedemos/Okul_Yonetim_Uygulama_Odevi"
W="C:/Users/taham/AppData/Local/Temp/claude/09d252a19f5bfcd1/2fc78961-704e-4a2f-9b3f-d7c2e5ec5024/scratchpad/verify"
export DOTNET_CLI_USE_MSBUILD_SERVER=0 DOTNET_CLI_UI_LANGUAGE=en

fresh() {
  rm -rf "$W"; mkdir -p "$W/packages/api-client"
  cp "$SRC/packages/api-client/openapi.json" "$W/packages/api-client/"
  (cd "$SRC" && tar --exclude='bin' --exclude='obj' -cf - backend) | (cd "$W" && tar -xf -)
}
build() { (cd "$W/backend" && dotnet build OkulYonetim.sln -c Release -nodeReuse:false --disable-build-servers -v:q -nologo 2>&1 | python -I "$W/../errcodes.py"); }
archtest() { (cd "$W/backend" && dotnet test --project tests/Architecture/Architecture.csproj -c Release --no-build 2>&1 | grep -E "^failed |  total:|  failed:" | head -6); }

echo "=== B1 (AC-6) bir dosyada ikinci tip: SystemHealthStatus.cs'e enum eklendi ==="
fresh; printf '\n/// <summary>x</summary>\npublic enum ExtraKind\n{\n    A,\n}\n' >> "$W/backend/src/Application/Features/SystemHealth/SystemHealthStatus.cs"; build

echo "=== B2 (AC-6, review I-8) bir dosyada iki record ==="
fresh; printf '\n/// <summary>x</summary>\npublic sealed record ExtraResponse(string A);\n' >> "$W/backend/src/Application/Features/SystemHealth/Queries/GetSystemHealth/GetSystemHealthQuery.cs"; build

echo "=== B3 (AC-6) dosya adı != tip adı: DatabaseMigration.cs -> Migrator.cs ==="
fresh; mv "$W/backend/src/Persistence/DatabaseMigration.cs" "$W/backend/src/Persistence/Migrator.cs"; build

echo "=== B4 (AC-7) kullanılmayan değişken (Domain ve Unit test projesi) ==="
fresh; sed -i 's#^{#{\n    private static void Unused()\n    {\n        int unused = 42;\n    }\n#' "$W/backend/src/Domain/DomainAssemblyMarker.cs"; build
fresh; f="$W/backend/tests/Unit/Logging/LoggingSetupTests.cs"; python -I -c "
import io,sys;p=sys.argv[1];s=io.open(p,encoding='utf-8').read();i=s.index('    [Fact]');s=s[:i]+'    private static void Unused()\n    {\n        int unused = 42;\n    }\n\n'+s[i:];io.open(p,'w',encoding='utf-8',newline='\n').write(s)" "$f"; build

echo "=== B5 (AC-8) summary boş: SerilogLoggerAdapter ==="
fresh; python -I -c "
import io,re,sys;p=sys.argv[1];s=io.open(p,encoding='utf-8').read();s=re.sub(r'/// <summary>.*?</summary>\n','/// <summary></summary>\n',s,count=1,flags=re.S);io.open(p,'w',encoding='utf-8',newline='\n').write(s)" "$W/backend/src/Infrastructure/Logging/SerilogLoggerAdapter.cs"; build; archtest

echo "=== B6 (AC-8) summary yok: DatabaseMigration ==="
fresh; python -I -c "
import io,re,sys;p=sys.argv[1];s=io.open(p,encoding='utf-8').read();s=re.sub(r'/// <summary>.*?</summary>\n','',s,count=1,flags=re.S);io.open(p,'w',encoding='utf-8',newline='\n').write(s)" "$W/backend/src/Persistence/DatabaseMigration.cs"; build; archtest

echo "=== B7 (AC-9) API katmanı OkulDbContext kullanır ==="
fresh; python -I -c "
import io,sys;p=sys.argv[1];s=io.open(p,encoding='utf-8').read();a='    public static void MigrateOkulDatabase';s=s.replace(a,'    public static System.Type Leak => typeof(OkulYonetim.Persistence.Contexts.OkulDbContext);\n\n'+a);io.open(p,'w',encoding='utf-8',newline='\n').write(s)" "$W/backend/src/Persistence/DatabaseMigration.cs"
cat > "$W/backend/src/WebAPI/Leak.cs" <<'EOF'
namespace OkulYonetim.WebAPI;

/// <summary>Kasıtlı bozma: API katmanı veritabanı bağlamına dokunur.</summary>
public static class Leak
{
    public static string Name() => typeof(OkulYonetim.Persistence.Contexts.OkulDbContext).Name;
}
EOF
build; archtest

echo "=== B8 (AC-9) Application -> Serilog (dış SDK) ==="
fresh
sed -i '0,/<\/ItemGroup>/s##  <PackageReference Include="Serilog" />\n  </ItemGroup>#' "$W/backend/src/Application/Application.csproj"
grep -q 'Include="Serilog"' "$W/backend/Directory.Packages.props" || sed -i 's#    <PackageVersion Include="Serilog.AspNetCore"#    <PackageVersion Include="Serilog" Version="4.3.0" />\n    <PackageVersion Include="Serilog.AspNetCore"#' "$W/backend/Directory.Packages.props"
cat > "$W/backend/src/Application/Leak.cs" <<'EOF'
namespace OkulYonetim.Application;

/// <summary>Kasıtlı bozma: Application dış log SDK'sına bağlanır.</summary>
public static class Leak
{
    public static string Name() => typeof(Serilog.ILogger).Name;
}
EOF
build; archtest
rm -rf "$W"
echo "=== bitti ==="
