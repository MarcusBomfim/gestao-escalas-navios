import { expect, type Page } from '@playwright/test'

type DemoProfile = 'Administrador' | 'Planejador' | 'Operador' | 'Visitante'

export async function signInAs(page: Page, profile: DemoProfile) {
  const password = process.env.DEMO_USER_PASSWORD
  if (!password) {
    throw new Error('DEMO_USER_PASSWORD precisa estar definida para os testes E2E.')
  }

  await page.getByRole('button', { name: profile, exact: true }).click()
  await page.getByLabel('Senha', { exact: true }).fill(password)
  await page.getByRole('button', { name: 'Entrar na plataforma' }).click()

  await expect(page).not.toHaveURL(/\/login$/)
}
