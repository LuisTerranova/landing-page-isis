# 🌿 Landing Page - Isis Vitória

[![.NET](https://img.shields.io/badge/.NET-9.0-512bd4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Blazor](https://img.shields.io/badge/Blazor-Interactive%20Server-512bd4?logo=blazor)](https://dotnet.microsoft.com/en-us/apps/aspnet/web-apps/blazor)
[![MudBlazor](https://img.shields.io/badge/MudBlazor-UI%20Framework-512bd4?logo=mudblazor)](https://mudblazor.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-latest-4169e1?logo=postgresql)](https://www.postgresql.org/)
[![Docker](https://img.shields.io/badge/Docker-ready-2496ed?logo=docker)](https://www.docker.com/)

Projeto Full-Stack desenvolvido para a psicóloga **Isis Vitória**, focado na captação de leads, acolhimento e gestão clínica completa de pacientes. A aplicação une uma interface pública elegante com um poderoso painel administrativo para gestão.

---

## ✨ Funcionalidades Principais

### 🌐 Área Pública
- **Landing Page Profissional:** Design focado em conversão e acolhimento.
- **Formulário de Contato/Leads:** Captura de intenção do paciente com limpeza automática de dados obsoletos.

### 🛡️ Painel Administrativo
- **Dashboard (Home):** Visualização rápida de métricas e próximos atendimentos.
- **Gestão de Pacientes:** Prontuário básico e histórico de sessões.
- **Controle de Agendamentos:** Calendário dinâmico com suporte a diferentes fusos horários.
- **Gestão Financeira:** Controle de recebimentos e fluxo de caixa do consultório.

### 🤖 Automação e Inteligência
- **Lembretes por E-mail:** Envio automático de lembretes de consulta via **Resend**.
- **Gestão de Timezones:** Tratamento inteligente entre o fuso horário de Porto Velho (GMT-4) e Brasília (GMT-3) para agendamentos e e-mails.

---

## 🛠️ Stack Tecnológica

### Backend & Core
- **Framework:** .NET 9.0 (ASP.NET Core)
- **Persistência:** Entity Framework Core
- **Banco de Dados:** PostgreSQL
- **Templates de E-mail:** RazorLight (Engine Razor para templates HTML)
- **Serviços em Background:** Hosted Services para automações e limpezas.

### Frontend
- **Interface:** Blazor Interactive Server
- **Componentes UI:** MudBlazor (Material Design)
- **Estilização:** CSS Customizado e Tematização Dinâmica.

### DevOps & Infraestrutura
- **Containerização:** Docker & Docker Compose
- **Hardware:** Otimizado para hospedagem em Raspberry Pi 5.
- **Autenticação:** Cookies Auth com Roles Administrativas (Seed automático de Admin).

---

## 🚀 Como Executar o Projeto (Quick Start)

A forma mais rápida e recomendada de rodar o projeto é utilizando **Docker**, que já configura automaticamente o banco de dados, a aplicação e as dependências.

### 1. Pré-requisitos
- [Docker](https://www.docker.com/) & [Docker Compose](https://docs.docker.com/compose/)
- Chave de API do [Resend](https://resend.com/) (para habilitar o envio de e-mails)

### 2. Configuração
1. Clone o repositório.
2. Crie o arquivo `.env` na raiz do projeto baseado no exemplo (ele é usado pelo Docker Compose):
   ```bash
   cp .env.example .env
   ```
3. Edite o arquivo `.env` e preencha as variáveis (especialmente as de administrador e a chave do Resend).

### 3. Rodar
Na raiz do projeto, execute:
```bash
docker-compose up -d
```
A aplicação estará disponível em `http://localhost:8080`. O banco de dados e as tabelas são criados automaticamente na primeira execução.

---

## 🛠️ Desenvolvimento e Customização

Se desejar rodar o projeto manualmente sem Docker para desenvolvimento:
- Certifique-se de ter o [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0) instalado.
- Configure um banco PostgreSQL local e atualize a string de conexão em `appsettings.Development.json`.
- Utilize o comando `dotnet ef database update` para gerenciar o esquema do banco via migrations.

---

## 📂 Estrutura da Solução
- `landing-page-isis/`: Aplicação Web principal (Blazor Server).
- `landing-page-isis.core/`: Lógica de domínio, modelos e interfaces.
- `landing-page-isis.tests/`: Testes unitários com xUnit.

---

## 📄 Licença e Uso
Este software é um projeto proprietário desenvolvido para a psicóloga autônoma Isis Vitória.

---
<p align="center">
  Desenvolvido com 🌿 por Luis Terranova
</p>
