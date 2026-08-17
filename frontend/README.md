# 前端说明

前端基于 Vue 3、TypeScript、Vite 和 Element Plus。这里保留项目实际需要的开发入口，不继续保留默认 Vite 模板说明。

## 常用命令

```bash
npm install
npm run dev
npm run build
npm run test
npm run test:coverage
npm run generate:api
```

## 本地开发

- 默认开发地址：`http://localhost:5173`
- 默认代理链路：前端通过 `/api` 访问后端
- 类型生成默认读取：`http://localhost:5187/swagger/v1/swagger.json`

## 相关说明

- 开发入门：[`../docs/04_Development/01_onboarding.md`](../docs/04_Development/01_onboarding.md)
- API 约定：[`../docs/03_API/01_api_conventions.md`](../docs/03_API/01_api_conventions.md)
- Swagger 示例：[`../docs/03_API/02_swagger_and_examples.md`](../docs/03_API/02_swagger_and_examples.md)
