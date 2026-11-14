// Her blok tek bir kuralın kasıtlı ihlalidir; RuleCatalog'daki beklenen tip adlarıyla eşleşir.
#pragma warning disable
namespace Konsept.Modules.Beta.Domain
{
    public class BetaThing { }
}

namespace Konsept.Modules.Beta.Infrastructure
{
    public class BetaDbContext : Microsoft.EntityFrameworkCore.DbContext { }
}

namespace Konsept.Modules.Alpha.Infrastructure
{
    public class AlphaRepository { }

    // Y-4: başka modülün DbContext'i.
    public class ForeignContextUser
    {
        public Konsept.Modules.Beta.Infrastructure.BetaDbContext? Context;
    }
}

namespace Konsept.Modules.Alpha.Application
{
    // Y-1: başka modülün Contracts dışı katmanı.
    public class CrossModuleUser
    {
        public Konsept.Modules.Beta.Domain.BetaThing? Thing;
    }

    // Y-3: Application → Infrastructure.
    public class InfraUser
    {
        public Konsept.Modules.Alpha.Infrastructure.AlphaRepository? Store;
    }
}

namespace Konsept.Modules.Alpha.Application.Concrete
{
    // K-2: Concrete altında base'siz, interface'siz sınıf.
    public class NakedService { }
}

namespace Konsept.Modules.Alpha.Domain
{
    // Y-2: Domain → EF Core.
    public class ImpureEntity
    {
        public Microsoft.EntityFrameworkCore.DbContext? Context;
    }

    // K-1: iç içe tip.
    public class Outer
    {
        public class Inner { }
    }
}

namespace Konsept.Modules.Alpha.Presentation
{
    // Y-6: controller → repository.
    public class RepositoryController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private readonly Konsept.Modules.Alpha.Infrastructure.AlphaRepository? _repository;
    }

    // K-4: controller içinde mantık metodu.
    public class FatController : Microsoft.AspNetCore.Mvc.ControllerBase
    {
        private int Compute() => 42;
    }
}

namespace Konsept.Web.Controllers
{
    // Y-5: Web → modül.
    public class LeakyWebController
    {
        public Konsept.Modules.Beta.Domain.BetaThing? Thing;
    }
}
