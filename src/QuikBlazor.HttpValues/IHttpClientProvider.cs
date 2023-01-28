using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuikBlazor.HttpValues;

public interface IHttpClientProvider
{
    HttpClient GetHttpClient(string? _httpClientName);
}
