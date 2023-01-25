using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HttpBindings;

public interface IHttpClientProvider
{
    HttpClient GetHttpClient(string? _httpClientName);
}
