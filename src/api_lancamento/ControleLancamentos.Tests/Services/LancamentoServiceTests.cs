using System.Threading.Tasks;
using Xunit;
using Moq;
using System.Net.Http;

namespace ControleLancamentos.Tests.Services
{
    public class LancamentoServiceTests
    {
        private readonly LancamentoService _lancamentoService;
        private readonly Mock<ILancamentoRepository> _lancamentoRepositoryMock;

        public LancamentoServiceTests()
        {
            _lancamentoRepositoryMock = new Mock<ILancamentoRepository>();

            var handlerMock = new Mock<HttpMessageHandler>();

            _lancamentoService = new LancamentoService(_lancamentoRepositoryMock.Object);
        }

        [Fact]
        public async Task AddLancamentoAsync_Should_Add_Lancamento()
        {
            var lancamento = new Lancamento {  Valor = 100, ClientId = 1, Tipo = TipoLancamento.Credito };

            await _lancamentoService.AddLancamentoAsync(lancamento);

            _lancamentoRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Lancamento>()), Times.Once);
        }
    }
}
