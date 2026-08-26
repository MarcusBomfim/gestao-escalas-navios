import { expect, test } from '@playwright/test'
import { signInAs } from './support/authentication.js'

test('apresenta o produto e informa que os dados são demonstrativos', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByRole('heading', { name: 'Clareza operacional para cada escala.' })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Acessar plataforma' })).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Informação certa, no momento certo.' })).toBeVisible()
  await expect(page.getByText('Projeto demonstrativo · Dados fictícios · 2026')).toBeVisible()
})

test('preserva o destino protegido, restaura a sessão e realiza logout', async ({ page }) => {
  await page.goto('/navios')

  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Entre na sua conta' })).toBeVisible()

  await signInAs(page, 'Planejador')

  await expect(page).toHaveURL(/\/navios$/)
  await expect(page.getByRole('heading', { name: 'Navios', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Cadastrar navio' })).toBeVisible()

  const navigation = page.getByRole('navigation', { name: 'Navegação principal' })
  await expect(navigation.getByRole('link', { name: 'Auditoria' })).toHaveCount(0)
  await expect(navigation.getByRole('link', { name: 'Saúde' })).toHaveCount(0)

  await page.reload()
  await expect(page).toHaveURL(/\/navios$/)
  await expect(page.getByText('Planejador Demo')).toBeVisible()

  await page.getByRole('button', { name: 'Sair' }).click()
  await expect(page).toHaveURL(/\/login$/)
  await expect(page.getByRole('heading', { name: 'Entre na sua conta' })).toBeVisible()
})

test('rejeita credenciais inválidas sem iniciar uma sessão', async ({ page }) => {
  await page.goto('/login')

  await page.getByLabel('E-mail', { exact: true }).fill('usuario.inexistente@portmanagement.local')
  await page.getByLabel('Senha', { exact: true }).fill('SenhaIncorreta!2026')
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click()

  await expect(page.getByRole('alert')).toHaveText('E-mail ou senha inválidos.')
  await expect(page).toHaveURL(/\/login$/)
})

test('mantém o perfil visitante em modo somente leitura', async ({ page }) => {
  await page.goto('/login')
  await signInAs(page, 'Visitante')

  await expect(page).toHaveURL(/\/painel$/)
  await expect(page.getByText('Visitante Demo')).toBeVisible()
  await expect(page.getByRole('heading', { name: 'Mapa operacional' })).toBeVisible()
  await expect(page.getByText('Dados simulados', { exact: true })).toBeVisible()
  await expect(page.getByText('não representam rastreamento AIS real')).toBeVisible()

  const navigation = page.getByRole('navigation', { name: 'Navegação principal' })
  await navigation.getByRole('link', { name: 'Navios' }).click()

  await expect(page).toHaveURL(/\/navios$/)
  await expect(page.getByRole('heading', { name: 'Navios', exact: true })).toBeVisible()
  await expect(page.getByRole('link', { name: 'Cadastrar navio' })).toHaveCount(0)
  await expect(navigation.getByRole('link', { name: 'Auditoria' })).toHaveCount(0)
  await expect(navigation.getByRole('link', { name: 'Saúde' })).toHaveCount(0)
})
