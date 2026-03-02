# 📦 Barcode Shipping System - Backend API

API REST desarrollada en **ASP.NET Core (.NET)** para la gestión y verificación de envíos mediante códigos de barras.

El sistema permite registrar, consultar y monitorear envíos, incluyendo endpoints para estados activos, históricos y estadísticas operativas, orientado a entornos logísticos y de control de inventario.

---

## 🚀 Stack Tecnológico

- ASP.NET Core (.NET 6/7)
- Entity Framework Core
- SQL Server
- Arquitectura en capas
- Docker
- Logging estructurado

---

## 🏗 Arquitectura

El proyecto está organizado bajo una arquitectura en capas:

### Ventajas

- Separación clara de responsabilidades
- Código mantenible y escalable
- Fácil extensión de funcionalidades
- Preparado para entornos productivos

---

## 📌 Funcionalidades Principales

- Registro de envíos mediante código de barras.
- Consulta de envíos activos.
- Consulta de historial completo.
- Endpoint de estadísticas operativas.
- Filtros por estado.
- Gestión estructurada de datos mediante DTOs.

---

## 📡 Endpoints Principales

### 📦 Registrar Envío

**POST** `/api/shipment`

```json
{
  "barcode": "1234567890",
  "destination": "Bodega Norte",
  "status": "Pending"
}
