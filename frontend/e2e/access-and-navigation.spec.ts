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
  await expect(navigation.getByRole('link', { name: 'Usuários' })).toHaveCount(0)
  await expect(navigation.getByRole('link', { name: 'Cadastros' })).toHaveCount(0)

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

test('solicita recuperação sem revelar se o e-mail está cadastrado', async ({ page }) => {
  await page.goto('/recuperar-senha')

  await page.getByLabel('E-mail', { exact: true }).fill('usuario.inexistente@portmanagement.local')
  await page.getByRole('button', { name: 'Enviar instruções' }).click()

  await expect(page.getByRole('status')).toContainText('Solicitação recebida')
  await expect(page.getByRole('status')).toContainText(
    'Se o e-mail estiver cadastrado, as instruções serão enviadas.',
  )
  await expect(page.getByRole('link', { name: 'Voltar para o login' })).toBeVisible()
})

test('mantém o perfil visitante em modo somente leitura', async ({ page }) => {
  await page.goto('/login')
  await page.getByRole('button', { name: 'Entrar como visitante' }).click()

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
  await expect(navigation.getByRole('link', { name: 'Usuários' })).toHaveCount(0)
  await expect(navigation.getByRole('link', { name: 'Cadastros' })).toHaveCount(0)
})

test('permite ao administrador consultar e iniciar o cadastro de usuários', async ({ page }) => {
  await page.goto('/login')
  await signInAs(page, 'Administrador')

  const navigation = page.getByRole('navigation', { name: 'Navegação principal' })
  await navigation.getByRole('link', { name: 'Usuários' }).click()

  await expect(page).toHaveURL(/\/usuarios$/)
  await expect(page.getByRole('heading', { name: 'Usuários e permissões' })).toBeVisible()
  await expect(page.getByText('admin.demo@portmanagement.local')).toBeVisible()

  await page.getByRole('button', { name: 'Cadastrar usuário' }).click()
  await expect(page.getByText('Cadastrar usuário', { exact: true }).last()).toBeVisible()
  await expect(page.getByLabel('Senha inicial')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Salvar alterações' })).toHaveCount(0)

  await page.getByRole('button', { name: 'Cancelar' }).click()
  await navigation.getByRole('link', { name: 'Cadastros' }).click()

  await expect(page).toHaveURL(/\/cadastros$/)
  await expect(page.getByRole('heading', { name: 'Cadastros mestres' })).toBeVisible()
  await expect(page.getByRole('button', { name: 'Cadastrar organização' })).toBeVisible()
  await page.getByRole('tab', { name: 'Portos, terminais e berços' }).click()
  await expect(page.getByRole('button', { name: 'Cadastrar porto' })).toBeVisible()
})
