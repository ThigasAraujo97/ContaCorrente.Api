/** Erro de negocio ou de transporte vindo da API, ja com mensagem exibivel. */
export class ApiError extends Error {
  readonly status: number;
  /** Erros por campo, quando a API devolve um ValidationProblemDetails (400). */
  readonly errosPorCampo?: Record<string, string[]>;

  constructor(
    message: string,
    status: number,
    errosPorCampo?: Record<string, string[]>,
  ) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.errosPorCampo = errosPorCampo;
  }

  /**
   * 422 e a resposta da API quando a regra de saldo recusa a saida. Nao e erro
   * de preenchimento: e o dominio dizendo nao.
   */
  get isSaldoInsuficiente(): boolean {
    return this.status === 422;
  }
}

const baseUrl = (import.meta.env.VITE_API_URL ?? '').replace(/\/$/, '');

interface ProblemDetails {
  title?: string;
  detail?: string;
  mensagem?: string;
  status?: number;
  errors?: Record<string, string[]>;
}

/** Traduz o corpo de erro da API (RFC 7807) em algo exibivel na tela. */
async function interpretarErro(resposta: Response): Promise<ApiError> {
  const generica = `Falha na requisicao (HTTP ${resposta.status}).`;
  const texto = await resposta.text().catch(() => '');

  if (!texto) return new ApiError(generica, resposta.status);

  let corpo: ProblemDetails | string;
  try {
    corpo = JSON.parse(texto) as ProblemDetails | string;
  } catch {
    return new ApiError(texto, resposta.status);
  }

  if (typeof corpo === 'string') return new ApiError(corpo, resposta.status);

  // Validacao vem por campo; junta tudo numa frase e preserva o detalhamento.
  if (corpo.errors) {
    const itens = Object.values(corpo.errors).flat();
    if (itens.length) {
      return new ApiError(itens.join(' '), resposta.status, corpo.errors);
    }
  }

  const mensagem = corpo.detail ?? corpo.mensagem ?? corpo.title ?? generica;
  return new ApiError(mensagem, resposta.status, corpo.errors);
}

export async function request<T>(caminho: string, init?: RequestInit): Promise<T> {
  let resposta: Response;

  try {
    resposta = await fetch(`${baseUrl}${caminho}`, {
      ...init,
      headers: { 'Content-Type': 'application/json', ...init?.headers },
    });
  } catch {
    throw new ApiError(
      'Nao foi possivel falar com a API. Verifique se ela esta em execucao.',
      0,
    );
  }

  if (!resposta.ok) {
    throw await interpretarErro(resposta);
  }

  if (resposta.status === 204) return undefined as T;

  return (await resposta.json()) as T;
}
